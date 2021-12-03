package com.example.practice;

import android.content.Context;
import android.content.Intent;
import android.Manifest;
import android.content.pm.PackageManager;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.os.Build;
import android.os.Environment;
import android.os.SystemClock;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.drawable.BitmapDrawable;
import android.os.Bundle;
import android.text.method.ScrollingMovementMethod;
import android.util.Log;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;
import android.text.TextUtils;

import java.io.File;
import java.nio.ByteBuffer;
import java.io.IOException;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.annotation.RequiresApi;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

//import org.nd4j.linalg.api.ndarray.INDArray;
//import org.nd4j.linalg.factory.Nd4j;
import org.tensorflow.lite.DataType;
import org.tensorflow.lite.gpu.CompatibilityList;
import org.tensorflow.lite.gpu.GpuDelegate;
import org.tensorflow.lite.support.image.ImageProcessor;
import org.tensorflow.lite.support.image.TensorImage;
import org.tensorflow.lite.support.image.ops.ResizeOp;
import org.tensorflow.lite.support.metadata.MetadataExtractor;
//import org.tensorflow.lite.HexagonDelegate;
import org.tensorflow.lite.support.common.TensorOperator;
import org.tensorflow.lite.support.common.ops.NormalizeOp;

import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Dictionary;
import java.util.HashMap;
import java.util.Hashtable;
import java.util.List;

import org.tensorflow.lite.Interpreter;

public class MainActivity extends AppCompatActivity {
    ImageView iv;
    Button gpuButton;
    Button cpuButton;
    Button floatButton;
    Button inferButton;
    Spinner spinner;
    TextView tv;
    Button gv;
    ListView lv;
    long h_time = 0;
    String result = "Result\n";
    boolean gpuUsage = false;
    boolean floatUsage = false;
    boolean imageReady = false;
    boolean modelReady = false;
    MetadataExtractor h_modelMeta;
    MetadataExtractor p_modelMeta;
    String h_modelName = "humandet";
    String p_modelName = "posenet";
    View previousSelected = null;
    int inferReps = 10;

    Interpreter h_interpreter;
    Interpreter p_interpreter;
    private GpuDelegate gpuDelegate = null;
    String [] keypoint_names = new String[] {
            "nose", "l_shoulder", "r_shoulder", "l_elbow", "r_elbow", "l_wrist", "r_wrist", "l_hip", "r_hip", "l_knee", "r_knee", "l_ankle", "r_ankle"
    };

    CompatibilityList compatList = new CompatibilityList();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        checkSelfPermission();

        tv = findViewById(R.id.textView1);
        tv.setMovementMethod(new ScrollingMovementMethod());
        tv.setText(result);

        iv = findViewById(R.id.iv);

        spinner = findViewById(R.id.spinner);
        final ArrayAdapter<CharSequence> adapter = ArrayAdapter.createFromResource(this, R.array.times, android.R.layout.simple_spinner_item);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spinner.setAdapter(adapter);
        spinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
                Object item = adapterView.getItemAtPosition(i);
                Log.d("item selected", String.valueOf(item));
                inferReps = Integer.valueOf(String.valueOf(item));
            }

            @Override
            public void onNothingSelected(AdapterView<?> adapterView) {
                inferReps = 10;
            }
        });

        lv = findViewById(R.id.list);
        File f = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS);
        final List<String> fileList = Arrays.asList(f.list());
        final List<String> modelList = new ArrayList<>();
        for (int p=0; p<fileList.size(); p+=1) {
            if (!fileList.get(p).contains(".jpg")) {
                modelList.add(fileList.get(p));
            }
        }
        final int fileLength = fileList.size();
        ArrayAdapter arrayAdapter = new ArrayAdapter(this, android.R.layout.simple_list_item_1, modelList.toArray());
        lv.setAdapter(arrayAdapter);
        lv.setOnItemClickListener(new AdapterView.OnItemClickListener() {
            @Override
            public void onItemClick(AdapterView<?> adapterView, View view, int i, long l) {
                if (previousSelected != null) previousSelected.setBackgroundColor(Color.WHITE);
                Log.d("Click!", modelList.get(i));
                view.setBackgroundColor(Color.LTGRAY);
                previousSelected = view;
                h_modelName = modelList.get(i);
            }
        });

        gpuButton = findViewById(R.id.gpu);
        cpuButton = findViewById(R.id.cpu);
        floatButton = findViewById(R.id.float16);
        cpuButton.setBackgroundColor(Color.WHITE);
        gpuButton.setBackgroundColor(Color.WHITE);
        floatButton.setBackgroundColor(Color.WHITE);

        gpuButton.setOnClickListener(new View.OnClickListener() {
            @RequiresApi(api = Build.VERSION_CODES.O)
            @Override
            public void onClick(View view) {
                if (h_modelName == "") return;
                gpuButton.setBackgroundColor(Color.LTGRAY);
                cpuButton.setBackgroundColor(Color.WHITE);
                floatButton.setBackgroundColor(Color.WHITE);
                gpuUsage = true;
                loadModel("gpu");
                modelReady = true;
                tv.setText(result);
            }
        });

        cpuButton.setOnClickListener(new View.OnClickListener() {
            @RequiresApi(api = Build.VERSION_CODES.O)
            @Override
            public void onClick(View view) {
                if (h_modelName == "") return;
                cpuButton.setBackgroundColor(Color.LTGRAY);
                gpuButton.setBackgroundColor(Color.WHITE);
                floatButton.setBackgroundColor(Color.WHITE);
                gpuUsage = false;
                loadModel("cpu");
                modelReady = true;
                tv.setText(result);
            }
        });

        floatButton.setOnClickListener(new View.OnClickListener() {
            @RequiresApi(api = Build.VERSION_CODES.O)
            @Override
            public void onClick(View view) {
                if (h_modelName == "") return;
                floatButton.setBackgroundColor(Color.LTGRAY);
                gpuButton.setBackgroundColor(Color.WHITE);
                cpuButton.setBackgroundColor(Color.WHITE);
                gpuUsage = true;
                floatUsage = true;
                loadModel("float16");
                modelReady = true;
                tv.setText(result);
            }
        });

        gv = findViewById(R.id.button_2);
        gv.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                // 기기 기본 갤러리 접근
                Intent intent = new Intent();
                intent.setType("image/*");
                intent.setAction(Intent.ACTION_GET_CONTENT);
                startActivityForResult(Intent.createChooser(intent, "Select Image"),100);

            }
        });

        inferButton = findViewById(R.id.infer);
        inferButton.setOnClickListener(new View.OnClickListener() {
            @RequiresApi(api = Build.VERSION_CODES.P)
            @Override
            public void onClick(View v) {
                if (!imageReady) return;
                if (!modelReady) return;
                result += "\nInference " + String.valueOf(inferReps) + " times";
                tv.setText(result);
                Bitmap result_bm = null;
                Bitmap bm = ((BitmapDrawable) iv.getDrawable()).getBitmap();
                result_bm = bm.copy(Bitmap.Config.ARGB_8888, true);
                int [] box = null;
                Dictionary<String, int []> pose = null;
                for (int i=0; i<inferReps; i+=1) {
                    if (i % 10 == 0) result += "\n";
                    long startTimeForReference = SystemClock.uptimeMillis();
                    box = h_modelInference(h_interpreter, bm, 0.5f);
                    Log.d("humandet:", "done");
                    pose = p_modelInference(p_interpreter, bm, box, 1.2f, 0.2f);
                    long endTimeForReference = SystemClock.uptimeMillis();
                    Log.d("Time cost of entire model inference (ms): ", String.valueOf(endTimeForReference - startTimeForReference));
//                    h_time += (endTimeForReference - startTimeForReference);
                }
                /*
                 * bounding box 및 skeleton 그리기
                 */
                if (box != null) {
                    Log.d("visualize", "start");
                    int[][] skels = {{0, 1}, {0, 2}, {1, 3}, {1, 7}, {2, 4}, {2, 8}, {3, 5}, {4, 6}, {7, 9}, {8, 10}, {9, 11}, {10, 12}};

                    /*
                     * bounding box 그리기
                     */
                    Paint paint = new Paint();
                    paint.setStrokeWidth(5);
                    paint.setStyle(Paint.Style.STROKE);
                    paint.setColor(Color.RED);
                    Canvas c = new Canvas(result_bm);
                    c.drawRect(box[0], box[1], box[0] + box[2], box[1] + box[3], paint); // (x1, y1), (x2, y2) format
                    /*
                     * 관절 좌표 점 찍기
                     */
                    paint.setColor(Color.BLUE);
                    paint.setStrokeWidth(10);
                    for (int j = 0; j < keypoint_names.length; j++) {
                        Log.d("key point name value", keypoint_names[j]);
                        int[] keypoint = pose.get(keypoint_names[j]);
                        if (keypoint[0] > -1 && keypoint[1] > -1) {
                            c.drawPoint(keypoint[0], keypoint[1], paint);
                        }
                    }
                    /*
                     * 관절 좌표들 잇기
                     */
                    paint.setStrokeWidth(4);
                    for (int k = 0; k < skels.length; k++) {
                        String start = keypoint_names[skels[k][0]];
                        String end = keypoint_names[skels[k][1]];
                        int[] start_point = pose.get(start);
                        int[] end_point = pose.get(end);
                        if (start_point[0] > -1 && start_point[1] > -1 && end_point[0] > -1 && end_point[1] > -1) {
                            c.drawLine(start_point[0], start_point[1], end_point[0], end_point[1], paint);
                        }
                    }
                    Log.d("visualize", "done");
                }
                iv.setImageBitmap(result_bm);
                Log.d("Average time cost of model inference (ms)", String.valueOf(h_time/inferReps));
                result += "\n" + "Average time cost of model inference (ms): " + String.valueOf(h_time/inferReps) +"\n";
                h_time = 0;
                tv.setText(result);

            }
        });
    }

    @RequiresApi(api = Build.VERSION_CODES.O)
    public void loadModel(String mode) {
        // Initialise the model
        /*
         * mode: 모델의 종류와 실행할 장치를 설정
         * float16 - fp16 모델들을 GPU에 load
         * gpu - fp32 모델들을 GPU에 load
         * cpu - fp32 모델들을 CPU에 load
         */
        try{
            String humandet = "";
            String posenet = "";
            /*
             * fp16인지 fp32인지에 따라 load 할 model 이름이 다름
             */
            if (mode == "float16"){
                humandet = h_modelName + "_fp16.tflite";
                posenet = p_modelName + "_fp16.tflite";
            } else {
                humandet = h_modelName + "_fp32.tflite";
                posenet = p_modelName + "_fp32.tflite";
            }
            long startTimeForReference = SystemClock.uptimeMillis();
            Context context = getApplicationContext();
            /*
             * humandet을 load하기 위한 메모리 할당
             */
            Log.d("TEst",context.getFilesDir().getAbsolutePath() + "/" + humandet);
            File h_model = new File(context.getFilesDir(), humandet);
            byte[] h_barray = Files.readAllBytes(Paths.get(context.getFilesDir().getAbsolutePath() + "/" + humandet));
//            byte[] h_barray = Files.readAllBytes(Paths.get("/data/user/0/hy_test/" + humandet));
            ByteBuffer h_bb = ByteBuffer.wrap(h_barray);
            h_modelMeta = new MetadataExtractor(h_bb);
            /*
             * posenet을 load하기 위한 메모리 할
             */
            File p_model = new File(context.getFilesDir(), posenet);
            byte[] p_barray = Files.readAllBytes(Paths.get(context.getFilesDir().getAbsolutePath() + "/" + posenet));
//            byte[] p_barray = Files.readAllBytes(Paths.get("/data/user/0/hy_test/" + posenet));
            ByteBuffer p_bb = ByteBuffer.wrap(p_barray);
            p_modelMeta = new MetadataExtractor(p_bb);
            /*
             * load한 model을 실행하기 위한 interpreter 생성
             * GPU에 model을 load할 경우 (mode가 "gpu" 또는 "float16") gpuDelegate를 사용
             */
            if (mode == "gpu") {
                Interpreter.Options interpreterOptions = new Interpreter.Options();
                gpuDelegate = new GpuDelegate(new GpuDelegate.Options().setPrecisionLossAllowed(false));
                interpreterOptions.addDelegate(gpuDelegate);
                Log.d("model", "GPU selected");
                h_interpreter = new Interpreter(h_model, interpreterOptions);
                p_interpreter = new Interpreter(p_model, interpreterOptions);
            } else if (mode == "cpu") {
                Log.d("model", "CPU selected");
                h_interpreter = new Interpreter(h_model);
                p_interpreter = new Interpreter(p_model);
            } else if (mode == "float16") {
                Log.d("model", "Float 16 selected");
                Interpreter.Options interpreterOptions = new Interpreter.Options();
                gpuDelegate = new GpuDelegate(new GpuDelegate.Options().setPrecisionLossAllowed(true));
                interpreterOptions.addDelegate(gpuDelegate);
                h_interpreter = new Interpreter(h_model, interpreterOptions);
                p_interpreter = new Interpreter(p_model, interpreterOptions);
            }
            /*
             * model load (init)에 걸린 시간 측정
             */
            long endTimeForReference = SystemClock.uptimeMillis();
            h_time = 0;

            result += "\n" + humandet + " and " + posenet + " selected in " + mode +" mode\n";
            result += "Time cost of model loading (ms): " + String.valueOf(endTimeForReference - startTimeForReference) + "\n";
        } catch (IOException e){
            Log.e("tfliteSupport", "Error reading model" + e);
        }
    }

    public int[] h_modelInference(Interpreter tflite, Bitmap bm, float threshold) {
        /*
         * 입력 데이터와 inference 결과를 주고 받을 버퍼를 만들기 위한 정보 수집
         */
        int[] inputTensorShape = h_modelMeta.getInputTensorShape(0);
        int outputTensorCount = h_modelMeta.getOutputTensorCount();
        DataType inputDataType;
        DataType outputDataType;
        if (h_modelMeta.getInputTensorType(0) == 0) {
            inputDataType = DataType.FLOAT32;
        } else {
            inputDataType = DataType.UINT8;
        }
        if (h_modelMeta.getOutputTensorType(0) == 0) {
            outputDataType = DataType.FLOAT32;
        } else {
            outputDataType = DataType.UINT8;
        }

        /*
         * 입력 이미지에 대한 전처리 함수 생성 및 전처리 수행
         * tensorflow-lite에 이미지를 전달하기 위한 Object 객체 생성
         */
        ImageProcessor imageProcessor =
                new ImageProcessor.Builder()
                        .add(new ResizeOp(inputTensorShape[1], inputTensorShape[2], ResizeOp.ResizeMethod.BILINEAR))
                        .add(getPreprocessNormalizeOp())
                        .build();
        TensorImage tImage = new TensorImage(inputDataType);
        tImage.load(bm);
        tImage = imageProcessor.process(tImage);
        Object[] inputImage = {tImage.getBuffer()};

        /*
         * humandet의 inference 결과를 받기위한 버퍼 생성
         */
        HashMap<Integer, Object> outputBuffer = new HashMap<>();
        float [][][][] output_h = new float [1][128][128][1];
        float [][][][] output_o = new float [1][128][128][2];
        float [][][][] output_s = new float [1][128][128][2];
        outputBuffer.put(0, output_h);
        outputBuffer.put(1, output_o);
        outputBuffer.put(2, output_s);

        /*
         * humandet inference 실행 및 시간 측정
         */
        if(null != tflite) {
            long startTimeForReference = SystemClock.uptimeMillis();
            tflite.runForMultipleInputsOutputs(inputImage, outputBuffer);
            long endTimeForReference = SystemClock.uptimeMillis();
            Log.d("Time cost of humandet model inference (ms): ", String.valueOf(endTimeForReference - startTimeForReference));
            result += "Time cost of humandet model inference (ms): " + String.valueOf(endTimeForReference - startTimeForReference) + "\n";
            h_time += (endTimeForReference - startTimeForReference);

            /*
             * humandet inference 결과 후처리 1
             * confidence score가 max인 지점 탐색
             */
            int x = 0;
            int y = 0;
            int count = 0;
//            Dictionary<String, int []> boxes = new Hashtable<String, int[]>();
//            float max = output_h[0][0][0][0];
            float max = -1.0f;
            float w_scaler = (float) bm.getWidth() / (float) inputTensorShape[2];
            float h_scaler = (float) bm.getHeight() / (float) inputTensorShape[1];
            long startTimeForReference2 = SystemClock.uptimeMillis();
            for(int i = 0; i < output_h[0].length; i++){
                for(int j = 0; j < output_h[0][0].length; j++){
                    // the output follows NHWC format, so i is y coordinate and j is x coordinate
//                    if(output_h[0][i][j][0] > threshold && max < output_h[0][i][j][0]){
//                        max = output_h[0][i][j][0];
//                        y = i;
//                        x = j;
//                    }
                    if(output_h[0][i][j][0] > threshold && max < output_h[0][i][j][0]){
                        max = output_h[0][i][j][0];
                        x = j;
                        y = i;
                    }
                }
            }
            /*
             * humandet inference 결과 후처리 2
             * denormalize 실행
             * posenet에 사람 영역을 쉽게 전달하기 위해 bounding box 좌표를 java bitmap format으로 변경
             *  x_center, y_center, width, height --> x1, y1, width, height
             */
            int width = (int)Math.floor(output_s[0][y][x][0] * w_scaler);
            int height = (int)Math.floor(output_s[0][y][x][1] * h_scaler);
            int x1 = (int)Math.floor(x * 4 * w_scaler)  - (width / 2);
            int y1 = (int)Math.floor(y * 4 * h_scaler)  - (height / 2);

            /*
             * humandet inference 결과 후처리 3
             * bounding box 영역 조절
             * 입력 이미지 크기에서 벗어날 경우 이미지에 맞게 resize
             */
            x1 = Math.max(0, x1);
            y1 = Math.max(0, y1);
            width = Math.min(width, bm.getWidth() - x1 - 1);
            height = Math.min(height, bm.getHeight() - y1 - 1);

            int [] box = new int[] {x1, y1, width, height};
            return box;

        }
        else{
            result += "Can not found humandet interpreter";
            return null;
        }

    }

    public Dictionary<String, int[]> p_modelInference(Interpreter tflite, Bitmap bm, int [] box, float margin, float threshold){
        /*
         * 입력 데이터와 inference 결과를 주고 받을 버퍼를 만들기 위한 정보 수집
         */
        int[] inputTensorShape = p_modelMeta.getInputTensorShape(0);
        int outputTensorCount = p_modelMeta.getOutputTensorCount();
        DataType inputDataType;
        DataType outputDataType;
        if (p_modelMeta.getInputTensorType(0) == 0) {
            inputDataType = DataType.FLOAT32;
        } else {
            inputDataType = DataType.UINT8;
        }
        if (p_modelMeta.getOutputTensorType(0) == 0) {
            outputDataType = DataType.FLOAT32;
        } else {
            outputDataType = DataType.UINT8;
        }
        /*
         * humandet에서 사람을 탐색하지 못했을 경우 posenet의 inference skip
         */
        if (box == null){
            result += "Can't find a human from the given image";
            return null;
        }
        else{
            /*
             * bounding box 확장
             */
            int x_center = box[0] + (box[2] / 2);
            int y_center = box[1] + (box[3] / 2);
            int new_width = (int)Math.ceil(box[2] * margin);
            int new_height = (int)Math.ceil(box[3] * margin);
            int new_x1 = Math.max(x_center - (new_width / 2), 0);
            int new_y1 = Math.max(y_center - (new_height / 2), 0);
            new_width = Math.min(new_width, bm.getWidth() - new_x1 - 1);
            new_height = Math.min(new_height, bm.getHeight() - new_y1 - 1);
            int [] new_box = new int[] {new_x1, new_y1, new_width, new_height};

            /*
             * posenet 입력 데이터 생성
             * 원본 이미지에서 humandet에서 찾은 bounding box 영역에 해당되는 부분을 복사하여 Bitmap 생성
             */
            Bitmap human = Bitmap.createBitmap(bm, new_box[0], new_box[1], new_box[2], new_box[3]);
            /*
             * 정확도를 높히기 위해 padding 추가
             */
            int x1 = new_box[0];
            int x2 = new_box[0] + new_box[2];
            int y1 = new_box[1];
            int y2 = new_box[1] + new_box[3];

            Log.d("human bbox x1: ", String.valueOf(x1) + ", " + String.valueOf(box[0]));
            Log.d("human bbox y1: ", String.valueOf(y1) + ", " + String.valueOf(box[1]));
            Log.d("human bbox x2: ", String.valueOf(x2) + ", " + String.valueOf(box[0] + box[2]));
            Log.d("human bbox y2: ", String.valueOf(y2) + ", " + String.valueOf(box[1] + box[3]));

            int bbox_long_side = Math.max(x2 - x1, y2 - y1);
            int border_top = (int)Math.floor((float)(bbox_long_side - (y2 - y1)) / (float) 2);
            int border_bottom = (int)Math.ceil((float)(bbox_long_side - (y2 - y1)) / (float) 2);
            int border_left = (int)Math.floor((float)(bbox_long_side - (x2 - x1)) / (float) 2);
            int border_right = (int)Math.ceil((float)(bbox_long_side - (x2 - x1)) / (float) 2);
            Bitmap padded_image = Bitmap.createBitmap(border_left + new_box[2] + border_right, border_top + new_box[3] +border_bottom, Bitmap.Config.ARGB_8888);
            Canvas canvas = new Canvas(padded_image);
            canvas.drawBitmap(human, border_left, border_top, null);
            iv.setImageBitmap(padded_image);
            /*
             * 입력 이미지에 대한 전처리 함수 생성 및 전처리 수행
             * tensorflow-lite에 이미지를 전달하기 위한 Object 객체 생성
             */
            ImageProcessor imageProcessor =
                    new ImageProcessor.Builder()
                            .add(new ResizeOp(inputTensorShape[1], inputTensorShape[2], ResizeOp.ResizeMethod.BILINEAR))
                            .add(getPreprocessNormalizeOp())
                            .build();
            TensorImage tImage = new TensorImage(inputDataType);
            tImage.load(padded_image);
            tImage = imageProcessor.process(tImage);
            Object[] inputImage = {tImage.getBuffer()};
            /*
             * posenet의 inference 결과를 받기위한 버퍼 생성
             */
            HashMap<Integer, Object> outputBuffer = new HashMap<>();
            float [][][][] heatmap = new float [1][56][56][13];
            outputBuffer.put(0, heatmap);

            /*
             * posenet inference 실행 및 시간 측정
             */
            if(null != tflite) {
                long startTimeForReference = SystemClock.uptimeMillis();
                tflite.runForMultipleInputsOutputs(inputImage, outputBuffer);
                long endTimeForReference = SystemClock.uptimeMillis();
                Log.d("Time cost of posenet model inference (ms): ", String.valueOf(endTimeForReference - startTimeForReference));
                result += "Time cost of posenet model inference (ms): " + String.valueOf(endTimeForReference - startTimeForReference) + "\n";
                h_time += (endTimeForReference - startTimeForReference);

                /*
                 * 13개 관절 노드에 대한 confidence score 값과 x,y 좌표를 저장할 버퍼 생성
                 */
                float [][] max = new float[heatmap[0][0][0].length][3];
                for (int k = 0; k < max.length; k++){
                    max[k][0] = heatmap[0][0][0][k]; // pred
                    max[k][1] = 0; //x coordinate
                    max[k][2] = 0; //y coordinate
                }
                /*
                 * posenet inference 결과 후처리 1
                 * 관절 노드 별로 가장 높은 confidence score와 해당되는 x,y 좌표 탐색
                 */
                for (int k = 0; k < heatmap[0][0][0].length; k++){
                    for (int i = 0; i < heatmap[0].length; i++) {
                        for (int j = 0; j < heatmap[0][0].length; j++) {
                            if (max[k][0] < heatmap[0][i][j][k]) {
                                max[k][0] = heatmap[0][i][j][k];
                                // the output follows NHWC format, so i is y coordinate and j is x coordinate
                                max[k][1] = j;
                                max[k][2] = i;
                            }
                        }
                    }
                }
                /*
                 * posenet inference 결과 후처리 2
                 * 정확도를 높이기 위해 max에 저장된 (x,y) 주변의 위치에서 local max를 탐색후 좌표값 조정
                 */
                float [][] neighbor_indices = {{-1, -1}, {-1, 0}, {-1, 1}, {0, -1}, {0, 1}, {-1, -1}, {-1, 0}, {-1, 1}};
                float [][] local_top1 = new float[heatmap[0][0][0].length][2];

                for (int i = 0; i < max.length; i++){
                    float local_max = -1;
                    for (int j = 0; j < neighbor_indices.length; j++){
                        int local_x = (int)(max[i][1] + neighbor_indices[0][0]);
                        int local_y = (int)(max[i][2] + neighbor_indices[0][1]);

                        if (local_x < 0 || local_y < 0 || local_x >= heatmap[0][0].length || local_y >= heatmap[0][0].length) continue;

                        if (local_max < heatmap[0][local_y][local_x][i]){
                            local_max = heatmap[0][local_y][local_x][i];
                            local_top1[i][0] = max[i][1] + 0.25f * neighbor_indices[0][0];
                            local_top1[i][1] = max[i][2] + 0.25f * neighbor_indices[0][1];
                        }
                    }
                }
                /*
                 * 관절 좌표 denormalize 및 저장
                 */
                Dictionary<String, int []> keypoint = new Hashtable<String, int []>();
                float scaler = (float)(inputTensorShape[2] - 1) / (float) bbox_long_side / 4.0f;
                for (int i = 0; i < local_top1.length; i++){
                    int x = -1, y = -1;
                    if (max[i][0] < threshold){
                        x = -1;
                        y = -1;
                        int [] coordinate = new int[] {x, y};
                        keypoint.put(keypoint_names[i], coordinate);
                        Log.d("real x value: ", keypoint_names[i] + ", "+ String.valueOf(coordinate[0]));
                        Log.d("real y value: ", keypoint_names[i] + ", "+ String.valueOf(coordinate[1]));
                    }
                    else {
                        x = (int)Math.floor(local_top1[i][0] / scaler) - border_left + new_box[0];
                        y = (int)Math.floor(local_top1[i][1] / scaler) - border_top + new_box[1];
                        int [] coordinate = new int[] {x, y};
                        keypoint.put(keypoint_names[i], coordinate);
                        Log.d("real x value: ", keypoint_names[i] + ", "+ String.valueOf(coordinate[0]));
                        Log.d("real y value: ", keypoint_names[i] + ", "+ String.valueOf(coordinate[1]));
                    }
                }
                return keypoint;
            }
            else{
                result += "Can not found posenet interpreter";
                return null;
            }
        }

    }

    private TensorOperator getPreprocessNormalizeOp() {
        return new NormalizeOp(1.0f, 127.5f);
//        if mode == 'tf':
//        x /= 127.5
//        x -= 1.
//        return x
    }

    public void checkSelfPermission() {

        String temp = "";

        //파일 읽기 권한 확인
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.READ_EXTERNAL_STORAGE)
                != PackageManager.PERMISSION_GRANTED) {
            temp += Manifest.permission.READ_EXTERNAL_STORAGE + " ";
        }

        if (TextUtils.isEmpty(temp) == false) {
            // 권한 요청
             ActivityCompat.requestPermissions(this, temp.trim().split(" "),1); }
        else {
            // 모두 허용 상태
             Toast.makeText(this, "권한을 모두 허용", Toast.LENGTH_SHORT).show();
        }

    }

    //권한에 대한 응답이 있을때 작동하는 함수
     @Override
     public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {

            //권한을 허용 했을 경우
          if(requestCode == 1){
              int length = permissions.length;
              for (int i = 0; i < length; i++) {
                  if (grantResults[i] == PackageManager.PERMISSION_GRANTED) {
                      // 동의
                       Log.d("MainActivity","권한 허용 : " + permissions[i]);
                  }
              }
          }
    }

    @RequiresApi(api = Build.VERSION_CODES.P)
    @Override
    protected void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == 100 && resultCode == RESULT_OK) {
            try {
                InputStream is = getContentResolver().openInputStream(data.getData());
                Bitmap bm = BitmapFactory.decodeStream(is);
                is.close();
                iv.setImageBitmap(bm);
                imageReady = true;
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }

}