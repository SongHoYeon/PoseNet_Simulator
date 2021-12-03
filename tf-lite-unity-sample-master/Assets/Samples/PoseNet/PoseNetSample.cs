using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TensorFlowLite;
using Cysharp.Threading.Tasks;
using UnityEngine.Android;
using System;
using System.Linq;

public class PoseNetSample : MonoBehaviour
{
    [SerializeField]
    private Text ui_score;
    [SerializeField]
    private Text ui_totalScore;
    public int score = 0;
    protected Camera m_Camera;
    protected LineRenderer m_leftLineRenderer;
    protected LineRenderer m_rightLineRenderer;
    protected EdgeCollider2D m_leftEdgeCollider2D;
    protected EdgeCollider2D m_rightEdgeCollider2D;
    protected List<Vector2> m_leftPoints;
    protected List<Vector2> m_rightPoints;
    public FruitSpawner fruitSpawner;
    public SceneController sceneController;

    [SerializeField, FilePopup("*.tflite")] string fileName = "posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
    [SerializeField] RawImage cameraView = null;
    [SerializeField, Range(0f, 1f)] float threshold = 0.5f;
    [SerializeField, Range(0f, 1f)] float lineThickness = 0.5f;


    [SerializeField]
    private Text logText;
    public float[] moveSpeeds;
    public WebCamTexture webcamTexture;
    PoseNet poseNet;
    Vector3[] corners = new Vector3[4];
    PrimitiveDraw draw;
    UniTask<bool> task;
    PoseNet.Result[] results;
    CancellationToken cancellationToken;
    public Vector2 leftPos;
    public Vector2 rightPos;
    public GameObject lLine;
    public GameObject rLine;
    private bool isUseFrontCam = true;
    private float time_start;

    void Awake()
    {
        Application.targetFrameRate = 40;

        time_start = Time.time;
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);

        WebCamDevice[] devices = WebCamTexture.devices;
        int selectCameraIdx = -1;
        if (logText != null)
            logText.text = devices.Length.ToString();
        for (int i = 0; i < devices.Length; i++)
        {
            if (logText != null)
                logText.text += '\n' + devices[i].name;
            if (devices[i].isFrontFacing == true)
                selectCameraIdx = i;
        }
        if (selectCameraIdx == -1)
        {
            isUseFrontCam = false;
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].isFrontFacing == false)
                    selectCameraIdx = i;
            }
        }

        if (selectCameraIdx >= 0)
        {
            webcamTexture = new WebCamTexture(devices[selectCameraIdx].name, 2960, 1440, 60);

            cameraView.texture = webcamTexture;
            webcamTexture.filterMode = FilterMode.Trilinear;
            webcamTexture.Play();

            if (!isUseFrontCam)
                cameraView.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        poseNet = new PoseNet(path);

        draw = new PrimitiveDraw()
        {
            color = Color.green,
        };

        cancellationToken = this.GetCancellationTokenOnDestroy();

        moveSpeeds = new float[2];

        if (lLine != null)
        {
            m_leftLineRenderer = lLine.GetComponent<LineRenderer>();
            m_rightLineRenderer = rLine.GetComponent<LineRenderer>();
        }
        if (rLine != null)
        {
            m_leftEdgeCollider2D = lLine.GetComponent<EdgeCollider2D>();
            m_rightEdgeCollider2D = rLine.GetComponent<EdgeCollider2D>();
        }

        m_Camera = Camera.main;
        m_leftPoints = new List<Vector2>();
        m_rightPoints = new List<Vector2>();
    }

    void OnDestroy()
    {
        if (webcamTexture != null)
            webcamTexture?.Stop();
        poseNet?.Dispose();
        draw?.Dispose();
    }

    bool callOnce = false;
    public float dist = 99;
    public float timer = 0;
    private Vector2 prevLeftPos;
    private Vector2 prevRightPos;
    public bool leftChk = false;
    public bool rightChk = false;
    public SpriteRenderer centerObj;
    public GameObject ResultPanel;
    public Text currentCount;
    public Text timerText;
    private bool isEnd = false;

    void Update()
    {
        if (webcamTexture == null)
            return;

        if (isEnd)
        {
            Invoke("InvokeMoveScene", 5f);
            ui_score.gameObject.active = false;
            ui_totalScore.gameObject.active = false;

            m_leftEdgeCollider2D.enabled = false;
            m_leftLineRenderer.positionCount = 0;
            m_leftEdgeCollider2D.Reset();
            if (m_leftPoints != null)
                m_leftPoints.Clear();
            m_rightEdgeCollider2D.enabled = false;
            m_rightLineRenderer.positionCount = 0;
            m_rightEdgeCollider2D.Reset();
            if (m_rightPoints != null)
                m_rightPoints.Clear();

            return;
        }
        dist = Vector2.Distance(leftPos, rightPos);
        if (sceneController.scene.name == "TestMode")
        {
            if (dist < 3f && leftPos != Vector2.zero && rightPos != Vector2.zero)
            {
                timer += Time.deltaTime;
                centerObj.enabled = true;
                centerObj.transform.localPosition = new Vector2((leftPos.x + rightPos.x) / 2, leftPos.y == 0 ? rightPos.y * 2 : leftPos.y * 2);
            }
            else
            {
                timer = 0;
                centerObj.enabled = false;
            }
            if (timer > 2f)
            {
                sceneController.Move();
            }
        }

        if (sceneController.scene.name == "GameMode")
        {
            if (fruitSpawner.GetFruitCount() == 50)
            {
                float timeCurrent = Time.time - time_start;
                fruitSpawner.StopFruitSpawn();
                ResultPanel.active = true;
                currentCount.text = score.ToString();
                timerText.text = $"{timeCurrent:N2}";
                isEnd = true;
            }

            if (!callOnce)
            {
                callOnce = true;
                print("Start");
                StartCoroutine(SpeedReckoner(0));
                StartCoroutine(SpeedReckoner(1));
            }

            ui_score.text = score.ToString();
            ui_totalScore.text = "/ " + fruitSpawner.GetFruitCount().ToString();
        }

        poseNet.Invoke(webcamTexture);
        results = poseNet.GetResults();
        cameraView.material = poseNet.transformMat;

        if (results != null && results.Length != 0)
        {
            Matrix4x4 mtx;

            if (isUseFrontCam)
                mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, true);
            else
                mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, true, true);

            for (int i = 0; i < results.Length; i++)
            {
                Vector3 p = mtx.MultiplyPoint3x4(new Vector3(results[i].x, results[i].y, 0));
                p = m_Camera.ViewportToWorldPoint(p);
                Vector2 newPos = new Vector2(p.x, p.y);

                if (results[i].part == PoseNet.Part.LEFT_WRIST)
                {
                    if (newPos.x > 10f || newPos.x < -10f || newPos.y > 3f || newPos.y < -3f)
                    {
                        leftPos = Vector2.zero;
                        continue;
                    }

                    if (Vector2.Distance(newPos, prevLeftPos) < 4f)
                        leftPos = newPos;

                    prevLeftPos = leftPos;

                }
                if (results[i].part == PoseNet.Part.RIGHT_WRIST)
                {
                    if (newPos.x > 10f || newPos.x < -10f || newPos.y > 3f || newPos.y < -3f)
                    {
                        rightPos = Vector2.zero;
                        continue;
                    }
                    if (Vector2.Distance(newPos, prevRightPos) < 4f)
                        rightPos = newPos;

                    prevRightPos = rightPos;
                }

            }
            PoseNet.Result leftSh = Array.Find(results, item => item.part == PoseNet.Part.LEFT_SHOULDER);
            PoseNet.Result rightSh = Array.Find(results, item => item.part == PoseNet.Part.RIGHT_SHOULDER);

            PoseNet.Result leftEL = Array.Find(results, item => item.part == PoseNet.Part.LEFT_ELBOW);
            PoseNet.Result rightEl = Array.Find(results, item => item.part == PoseNet.Part.RIGHT_ELBOW);

            PoseNet.Result leftWR = Array.Find(results, item => item.part == PoseNet.Part.LEFT_WRIST);
            PoseNet.Result rightWR = Array.Find(results, item => item.part == PoseNet.Part.RIGHT_WRIST);

            PoseNet.Result leftHp = Array.Find(results, item => item.part == PoseNet.Part.LEFT_HIP);
            PoseNet.Result rightHp = Array.Find(results, item => item.part == PoseNet.Part.RIGHT_HIP);

            PoseNet.Result leftKn = Array.Find(results, item => item.part == PoseNet.Part.LEFT_KNEE);
            PoseNet.Result rightKn = Array.Find(results, item => item.part == PoseNet.Part.RIGHT_KNEE);

            PoseNet.Result leftAn = Array.Find(results, item => item.part == PoseNet.Part.LEFT_ANKLE);
            PoseNet.Result rightAn = Array.Find(results, item => item.part == PoseNet.Part.RIGHT_ANKLE);

            PoseNet.Result tmp;
            if (leftSh.x > rightSh.x)
            {
                tmp = leftSh;
                leftSh = rightSh;
                rightSh = tmp;
            }
            if (leftEL.x > rightEl.x)
            {
                tmp = leftEL;
                leftEL = rightEl;
                rightEl = tmp;
            }
            if (leftWR.x > rightWR.x)
            {
                tmp = leftWR;
                leftWR = rightWR;
                rightWR = tmp;
            }
            if (leftHp.x > rightHp.x)
            {
                tmp = leftHp;
                leftHp = rightHp;
                rightHp = tmp;
            }
            if (leftKn.x > rightKn.x)
            {
                tmp = leftKn;
                leftKn = rightKn;
                rightKn = tmp;
            }
            if (leftAn.x > rightAn.x)
            {
                tmp = leftAn;
                leftAn = rightAn;
                rightAn = tmp;
            }

            DrawResult();

            if (sceneController.scene.name == "GameMode")
            {
                if (moveSpeeds[0] > 5f)
                {
                    m_leftEdgeCollider2D.enabled = true;

                    if (!m_leftPoints.Contains(leftPos) && !m_rightPoints.Contains(leftPos))
                    {
                        if (leftPos != rightPos)
                        {
                            m_leftPoints.Add(leftPos);
                        }
                        m_leftLineRenderer.positionCount = m_leftPoints.Count;
                        m_leftLineRenderer.SetPosition(m_leftLineRenderer.positionCount - 1, leftPos);
                        if (m_leftEdgeCollider2D != null && m_leftPoints.Count > 1)
                        {
                            m_leftEdgeCollider2D.points = m_leftPoints.ToArray();
                        }
                    }
                }
                else
                {
                    m_leftEdgeCollider2D.enabled = false;
                    m_leftLineRenderer.positionCount = 0;
                    m_leftEdgeCollider2D.Reset();

                    if (m_leftPoints != null)
                    {
                        m_leftPoints.Clear();
                    }
                }

                if (moveSpeeds[1] > 5f)
                {
                    m_rightEdgeCollider2D.enabled = true;
                    if (!m_rightPoints.Contains(rightPos) && !m_leftPoints.Contains(rightPos))
                    {
                        if (leftPos != rightPos)
                            m_rightPoints.Add(rightPos);
                        m_rightLineRenderer.positionCount = m_rightPoints.Count;
                        m_rightLineRenderer.SetPosition(m_rightLineRenderer.positionCount - 1, rightPos);
                        if (m_rightEdgeCollider2D != null && m_rightPoints.Count > 1)
                        {
                            m_rightEdgeCollider2D.points = m_rightPoints.ToArray();
                        }
                    }
                }
                else
                {
                    m_rightEdgeCollider2D.enabled = false;
                    m_rightLineRenderer.positionCount = 0;
                    m_rightEdgeCollider2D.Reset();

                    if (m_rightPoints != null)
                    {
                        m_rightPoints.Clear();
                    }
                }
            }
        }
    }

    private IEnumerator SpeedReckoner(int idx)
    {
        YieldInstruction timedWait = new WaitForSeconds(0.1f);


        Vector3 pos;
        Vector3 lastPos;
        if (idx == 0)
        {
            pos = new Vector3(leftPos.x, leftPos.y, 0f);
            lastPos = new Vector3(leftPos.x, leftPos.y, 0f);
        }
        else
        {
            pos = new Vector3(rightPos.x, rightPos.y, 0f);
            lastPos = new Vector3(rightPos.x, rightPos.y, 0f);
        }
        float lastTimestamp = Time.time;
        while (enabled)
        {
            if (idx == 0)
                pos = new Vector3(leftPos.x, leftPos.y, 0f);
            else
                pos = new Vector3(rightPos.x, rightPos.y, 0f);

            yield return timedWait;
            var deltaPos = (pos - lastPos).magnitude;
            var deltaTime = Time.time - lastTimestamp;
            if (Mathf.Approximately(deltaPos, 0f))
                deltaPos = 0f;
            moveSpeeds[idx] = deltaPos / deltaTime;
            lastPos = pos;
            lastTimestamp = Time.time;
        }
    }

    void DrawResult()
    {
        var rect = cameraView.GetComponent<RectTransform>();
        rect.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];

        var connections = PoseNet.Connections;
        int len = connections.GetLength(0);
        for (int i = 0; i < len; i++)
        {
            var a = results[(int)connections[i, 0]];
            var b = results[(int)connections[i, 1]];
            if (a.confidence >= threshold && b.confidence >= threshold)
            {
                draw.Line3D(
                    MathTF.Lerp(min, max, new Vector3(a.x, 1f - a.y, 0)),
                    MathTF.Lerp(min, max, new Vector3(b.x, 1f - b.y, 0)),
                    lineThickness
                );
            }
        }

        draw.Apply();
    }

    void InvokeMoveScene()
    {
        sceneController.Move();
    }
    async UniTask<bool> InvokeAsync()
    {
        results = await poseNet.InvokeAsync(webcamTexture, cancellationToken);
        cameraView.material = poseNet.transformMat;
        return true;
    }

}
