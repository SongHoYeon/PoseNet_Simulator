using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Android;
/// <summary>
/// BlazePose form MediaPipe
/// https://github.com/google/mediapipe
/// https://viz.mediapipe.dev/demo/pose_tracking
/// </summary>
public sealed class BlazePoseSample : MonoBehaviour
{


    [SerializeField]
    private Text ui_score;
    private int score = 0;
    protected LineRenderer m_LineRenderer;
    protected EdgeCollider2D m_EdgeCollider2D;
    protected Camera m_Camera;
    protected List<Vector2> m_Points;
    public FruitSpawner fruitSpawner;
    public SceneController sceneController;
    [SerializeField, FilePopup("*.tflite")] string poseDetectionModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField, FilePopup("*.tflite")] string poseLandmarkModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField] RawImage cameraView = null;
    [SerializeField] Canvas canvas = null;
    [SerializeField] bool useLandmarkFilter = true;
    [SerializeField] Vector3 filterVelocityScale = Vector3.one * 10;

    [SerializeField, Range(0f, 1f)] float visibilityThreshold = 0.5f;
    [SerializeField]
    private Text logText;


    public WebCamTexture webcamTexture;
    PoseDetect poseDetect;
    PoseLandmarkDetect poseLandmark;

    Vector3[] rtCorners = new Vector3[4]; // just cache for GetWorldCorners
    // [SerializeField] // for debug raw data'
    public Vector4[] worldJoints;
    public float[] moveSpeeds;
    PrimitiveDraw draw;
    PoseDetect.Result poseResult;
    PoseLandmarkDetect.Result landmarkResult;
    UniTask<bool> task;
    CancellationToken cancellationToken;

    bool NeedsDetectionUpdate => poseResult == null || poseResult.score < 0.5f;
    public Vector2 leftPos;
    public Vector2 rightPos;
    void Awake()
    {
        // if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        // {
        //     Permission.RequestUserPermission(Permission.Camera);

        // }

        // WebCamDevice[] devices = WebCamTexture.devices;
        // int selectCameraIdx = -1;
        // if (logText != null)
        //     logText.text = devices.Length.ToString();
        // for (int i = 0 ; i < devices.Length ; i ++) {
        //     if (logText != null)
        //         logText.text += '\n' + devices[i].name;
        //     if (devices[i].isFrontFacing == true) {
        //         selectCameraIdx = i;
        //         break;
        //     }
        // }
        // if (selectCameraIdx >= 0) {

        //     webcamTexture = new WebCamTexture(devices[selectCameraIdx].name,2960, 1440, 60);
        //     cameraView.texture = webcamTexture;
        //     webcamTexture.filterMode = FilterMode.Trilinear;
        //     webcamTexture.Play();
        //     // if (logText != null)
        //     //     logText.text += "\n" + devices[selectCameraIdx].name;
        // }
    }
    void Start()
    {
        // Init model
        poseDetect = new PoseDetect(poseDetectionModelFile);
        poseLandmark = new PoseLandmarkDetect(poseLandmarkModelFile);

        // Init camera 
        // string cameraName = WebCamUtil.FindName(new WebCamUtil.PreferSpec()
        // {
        //     isFrontFacing = false,
        //     kind = WebCamKind.WideAngle,
        // });
        // webcamTexture = new WebCamTexture(cameraName, Screen.width, Screen.height, 30);
        // cameraView.texture = webcamTexture;
        // webcamTexture.Play();
        webcamTexture = GetComponent<PoseNetSample>().webcamTexture;

        draw = new PrimitiveDraw(Camera.main, gameObject.layer);
        worldJoints = new Vector4[PoseLandmarkDetect.JointCount];
        // moveSpeeds = new float[2];
        cancellationToken = this.GetCancellationTokenOnDestroy();

        // m_LineRenderer = gameObject.AddComponent<LineRenderer>();
        // m_LineRenderer.positionCount = 0;
        // m_LineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        // m_LineRenderer.startColor = Color.red;
        // m_LineRenderer.endColor = Color.red;
        // m_LineRenderer.startWidth = 0.2f;
        // m_LineRenderer.endWidth = 0.2f;
        // m_LineRenderer.useWorldSpace = true;

        // m_EdgeCollider2D = gameObject.AddComponent<EdgeCollider2D>();
        // m_EdgeCollider2D.isTrigger = true;
        // m_EdgeCollider2D.enabled = false;
        m_Camera = Camera.main;
        // m_Points = new List<Vector2>();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Fruit")
        {
            // col.GetComponent<FruitObject>().DestroyObject();
            score++;
        }
    }
    void OnDestroy()
    {
        if (webcamTexture != null)
            webcamTexture?.Stop();
        poseDetect?.Dispose();
        poseLandmark?.Dispose();
        draw?.Dispose();
    }

    bool isStart = false;
    bool callOnce = false;
    public float threshold = 4f;
    public float dist = 99;
    public float timer = 0;
    void Update()
    {
        if (webcamTexture == null)
            return;
        if (worldJoints == null)
            Debug.Log("FFFFFFFFFF");
        // Vector2 firstPosition = new Vector2(worldJoints[19].x, worldJoints[19].y);
        // Vector2 secondPosition = new Vector2(worldJoints[20].x, worldJoints[20].y);

        // if (startFlag && isStart)
        //     dist = Vector2.Distance(firstPosition, secondPosition);
        // if (dist < 3f)
        // {
        //     timer += Time.deltaTime;
        // }
        // else
        // {
        //     timer = 0;
        // }
        // if (timer > 2f)
        // {
        //     sceneController.Move();
        // }

        // if (ui_score != null)
        // {
        //     if (isStart)
        //     {
        //         if (!callOnce)
        //         {
        //             callOnce = true;
        //             print("Start");
        //             StartCoroutine(SpeedReckoner(0));
        //             StartCoroutine(SpeedReckoner(1));
        //         }
        //     }

        //     ui_score.text = score.ToString();


        //     if (moveSpeeds[0] > threshold)
        //     {
        //         m_EdgeCollider2D.enabled = true;
        //         // Vector2 mousePosition = m_Camera.ScreenToWorldPoint(Input.mousePosition);

        //         if (!m_Points.Contains(firstPosition))
        //         {
        //             m_Points.Add(firstPosition);
        //             m_LineRenderer.positionCount = m_Points.Count;
        //             m_LineRenderer.SetPosition(m_LineRenderer.positionCount - 1, firstPosition);
        //             if (m_EdgeCollider2D != null && m_Points.Count > 1)
        //             {
        //                 m_EdgeCollider2D.points = m_Points.ToArray();
        //             }
        //         }
        //     }
        //     else if (moveSpeeds[1] > threshold)
        //     {
        //         m_EdgeCollider2D.enabled = true;
        //         // Vector2 mousePosition = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        //         if (!m_Points.Contains(secondPosition))
        //         {
        //             m_Points.Add(secondPosition);
        //             m_LineRenderer.positionCount = m_Points.Count;
        //             m_LineRenderer.SetPosition(m_LineRenderer.positionCount - 1, secondPosition);
        //             if (m_EdgeCollider2D != null && m_Points.Count > 1)
        //             {
        //                 m_EdgeCollider2D.points = m_Points.ToArray();
        //             }
        //         }
        //     }
        //     else
        //     {
        //         m_EdgeCollider2D.enabled = false;
        //         if (m_LineRenderer != null)
        //         {
        //             m_LineRenderer.positionCount = 0;
        //         }
        //         if (m_Points != null)
        //         {
        //             m_Points.Clear();
        //         }
        //         if (m_EdgeCollider2D != null)
        //         {
        //             m_EdgeCollider2D.Reset();
        //         }
        //     }
        // }



        Invoke();

        if (poseResult != null && poseResult.score > 0f)
        {
            DrawFrame(poseResult);
        }

        if (landmarkResult != null && landmarkResult.score > 0.2f)
        {
            // DrawCropMatrix(poseLandmark.CropMatrix);
            DrawJoints(landmarkResult.joints);
        }
    }

    void DrawFrame(PoseDetect.Result pose)
    {
        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        draw.color = Color.green;
        draw.Rect(MathTF.Lerp(min, max, pose.rect, true), 0.02f, min.z);

        foreach (var kp in pose.keypoints)
        {
            draw.Point(MathTF.Lerp(min, max, (Vector3)kp, true), 0.05f);
        }
        draw.Apply();
    }

    void DrawCropMatrix(in Matrix4x4 matrix)
    {
        draw.color = Color.red;

        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        var mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored)
            * matrix.inverse;
        Vector3 a = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 0, 0)));
        Vector3 b = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 0, 0)));
        Vector3 c = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 1, 0)));
        Vector3 d = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 1, 0)));

        draw.Quad(a, b, c, d, 0.02f);
        draw.Apply();
    }

    Vector3 prevJoint1 = new Vector4(0, 0, 0, 0);
    Vector3 prevJoint2 = new Vector4(0, 0, 0, 0);
    void DrawJoints(Vector4[] joints)
    {
        draw.color = Color.blue;

        // Vector3 min = rtCorners[0];
        // Vector3 max = rtCorners[2];
        // Debug.Log($"rtCorners min: {min}, max: {max}");

        // Apply webcam rotation to draw landmarks correctly
        Matrix4x4 mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored);

        // float zScale = (max.x - min.x) / 2;
        float zScale = 1;
        float zOffset = canvas.planeDistance;
        float aspect = (float)Screen.width / (float)Screen.height;
        Vector3 scale, offset;
        if (aspect > 1)
        {
            scale = new Vector3(1f / aspect, 1f, zScale);
            offset = new Vector3((1 - 1f / aspect) / 2, 0, zOffset);
        }
        else
        {
            scale = new Vector3(1f, aspect, zScale);
            offset = new Vector3(0, (1 - aspect) / 2, zOffset);
        }

        // Update world joints
        // var camera = canvas.worldCamera;
        var camera = m_Camera;
        for (int i = 19; i <= 20; i++)
        {
            Vector2 p1;
            Vector2 p2;



            Vector3 p = mtx.MultiplyPoint3x4((Vector3)joints[i]);
            // p = Vector3.Scale(p, scale) + offset;
            p = camera.ViewportToWorldPoint(p);


            if (i == 19)
                p1 = new Vector2(prevJoint1.x, prevJoint1.y);
            else
                p1 = new Vector2(prevJoint2.x, prevJoint2.y);
            p2 = new Vector2(p.x, p.y);

            float dist = Vector2.Distance(p1, p2);

            if (dist > 10f)
            {
                if (i == 19)
                {
                    worldJoints[i] = new Vector4(prevJoint1.x, prevJoint1.y, p.z, joints[i].w);
                    leftPos = new Vector4(prevJoint1.x, prevJoint1.y, p.z, joints[i].w);
                }

                else
                {
                    worldJoints[i] = new Vector4(prevJoint2.x, prevJoint2.y, p.z, joints[i].w);
                    rightPos = new Vector4(prevJoint2.x, prevJoint2.y, p.z, joints[i].w);
                }

            }
            else
            {
                // w is visibility
                worldJoints[i] = new Vector4(p.x, p.y, p.z, joints[i].w);
                if (i == 19)
                {
                    leftPos = new Vector4(prevJoint1.x, prevJoint1.y, p.z, joints[i].w);
                }

                else
                {
                    rightPos = new Vector4(prevJoint2.x, prevJoint2.y, p.z, joints[i].w);
                }
            }
        }


        Vector4 t_p = worldJoints[19];
        if (t_p.w > visibilityThreshold)
        {
            draw.Cube(t_p, 0.2f);
        }


        t_p = worldJoints[20];
        if (t_p.w > visibilityThreshold)
        {
            draw.Cube(t_p, 0.2f);
        }

        prevJoint1 = worldJoints[19];
        prevJoint2 = worldJoints[20];

        // Draw
        // for (int i = 0; i < worldJoints.Length; i++)
        // {
        //     Vector4 p = worldJoints[i];
        //     if (p.w > visibilityThreshold)
        //     {
        //         draw.Cube(p, 0.2f);
        //     }
        // }

        // var connections = PoseLandmarkDetect.Connections;
        draw.color = Color.red;
        // for (int i = 0; i < connections.Length; i += 2)
        // {
        //     var a = worldJoints[connections[i]];
        //     var b = worldJoints[connections[i + 1]];
        //     if (a.w > visibilityThreshold || b.w > visibilityThreshold)
        //     {
        //         draw.Line3D(a, b, 0.05f);
        //     }
        // }
        if (ui_score != null)
            draw.Apply();
    }

    // public float UpdateDelay;
    private IEnumerator SpeedReckoner(int idx)
    {
        YieldInstruction timedWait = new WaitForSeconds(0.1f);

        Vector3 pos = new Vector3(worldJoints[idx + 19].x, worldJoints[idx + 19].y, 0f);
        Vector3 lastPos = new Vector3(worldJoints[idx + 19].x, worldJoints[idx + 19].y, 0f);
        float lastTimestamp = Time.time;
        while (enabled)
        {
            pos = new Vector3(worldJoints[idx + 19].x, worldJoints[idx + 19].y, 0f);
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


    private bool startFlag = false;
    void Invoke()
    {
        if (NeedsDetectionUpdate)
        {
            poseDetect.Invoke(webcamTexture);
            cameraView.material = poseDetect.transformMat;
            cameraView.rectTransform.GetWorldCorners(rtCorners);
            poseResult = poseDetect.GetResults(0.7f, 0.3f);
        }
        if (poseResult.score < 0)
        {
            poseResult = null;
            landmarkResult = null;
            startFlag = false;
            dist = 99;
            if (fruitSpawner != null)
                fruitSpawner.SetFindUser(false);
            return;
        }
        poseLandmark.Invoke(webcamTexture, poseResult);
        if (fruitSpawner != null)
            fruitSpawner.SetFindUser(true);
        if (!isStart)
            isStart = true;
        startFlag = true;
        if (useLandmarkFilter)
        {
            poseLandmark.FilterVelocityScale = filterVelocityScale;
        }
        landmarkResult = poseLandmark.GetResult(useLandmarkFilter);

        if (landmarkResult.score < 0.3f)
        {
            poseResult.score = landmarkResult.score;
        }
        else
        {
            poseResult = PoseLandmarkDetect.LandmarkToDetection(landmarkResult);
        }
    }

    async UniTask<bool> InvokeAsync()
    {
        if (NeedsDetectionUpdate)
        {
            // Note: `await` changes PlayerLoopTiming from Update to FixedUpdate.
            poseResult = await poseDetect.InvokeAsync(webcamTexture, cancellationToken, PlayerLoopTiming.FixedUpdate);
        }
        if (poseResult.score < 0)
        {
            poseResult = null;
            landmarkResult = null;
            return false;
        }

        if (useLandmarkFilter)
        {
            poseLandmark.FilterVelocityScale = filterVelocityScale;
        }
        landmarkResult = await poseLandmark.InvokeAsync(webcamTexture, poseResult, useLandmarkFilter, cancellationToken, PlayerLoopTiming.Update);

        // Back to the update timing from now on 
        if (cameraView != null)
        {
            cameraView.material = poseDetect.transformMat;
            cameraView.rectTransform.GetWorldCorners(rtCorners);
        }

        // Generate poseResult from landmarkResult
        if (landmarkResult.score < 0.3f)
        {
            poseResult.score = landmarkResult.score;
        }
        else
        {
            poseResult = PoseLandmarkDetect.LandmarkToDetection(landmarkResult);
        }

        return true;
    }
}
