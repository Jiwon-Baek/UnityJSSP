using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;  // for File I/O
using System.Globalization;  // for converting strings to floats

using System.Linq;
using System;

class Job
{
    public Job(Color _color, int _blockindex, GameObject prefab, 
        List<Vector3> _positions, Vector3 _source, Vector3 _sink, 
        float[] _movetime, float[] _finishtime, float[] _starttime)
    {
        
        // Instantiate는 static 메서드이므로 Object.Instantiate로 호출해야 합니다.
        block = UnityEngine.Object.Instantiate(prefab);
        // 생성한 인스턴스에서 Block 컴포넌트 가져오기
        Block blockComp = block.GetComponent<Block>();
        // 초기화 메서드 호출
        blockComp.Initialize(_color, _blockindex, 
            _positions, _source, _sink,
            _movetime, _finishtime, _starttime);

        // 활성화
        block.SetActive(true);

    }

    public GameObject block;

}
class Machine
{
    public Machine(GameObject prefab, Vector3 initialPosition)
    {

        // Instantiate는 static 메서드이므로 Object.Instantiate로 호출해야 합니다.
        process = UnityEngine.Object.Instantiate(prefab);
        // 생성한 인스턴스에서 Block 컴포넌트 가져오기
        Process processComp = process.GetComponent<Process>();
        processComp.transform.position = initialPosition;

        process.SetActive(true);
    }
    public GameObject process;

}




public class GameManager : MonoBehaviour
{
    public GameObject BlockPrefab;
    public GameObject ProcessPrefab;


    public string colorPath = "color.csv"; // CSV 파일 경로 (프로젝트 폴더 내)
    public string positionPath = "position.csv";
    public string timePath = "ATCS.csv";

    private List<Color> colorData; // CSV 파일로부터 읽은 RGB 값들 저장
    private List<Vector3> positionData;
    private List<(int idx, int machine, float move, float start, float finish)> timeData;

    public Transform parent;
    static int numBlocks = 10;
    static int numProcesses = 5+3;
    private int sink_idx;

    public bool isFinished;

    // Job 객체 배열 선언
    private Job[] jobs;  // Job[] 타입으로 선언
    private Machine[] machines;
    private Vector3 pos;
    private Vector3 processposition;
    private Vector3 machineposition;
    int count;
    float movingtime = 0.5f;
    List<Vector3> source;
    List<Vector3> sink;
    private float timer = 0.0f;
    int num_created;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 2f;
        // 1. CSV 파일 읽기
        // 1. CSV 파일 읽기
        timeData = ReadTimeTableFromCSV(timePath);
        colorData = ReadColorsFromCSV(colorPath);
        positionData = ReadPositionFromCSV(positionPath);
        num_created = 0;
        sink_idx = 0;

        // Job 배열 크기 지정
        jobs = new Job[numBlocks];  // 3개의 Job을 담을 수 있는 배열 생성
        machines = new Machine[numProcesses];

        source = stackPositions(5, 0.0f, 0.0f, 1.5f, 0.6f, -0.6f);
        sink = stackPositions(5, 15.0f, 0.0f, -2.0f, -0.6f, -0.6f);

        List<float[]> move = new List<float[]>();
        List<float[]> start = new List<float[]>();
        List<float[]> finish = new List<float[]>();
        float interval = 2.0f;
        List<float> t = new List<float>();
        for (int u = 0; u < numProcesses; u++)
        {
            t.Add(interval * u);
        }

        for (int v = 0; v< numBlocks; v++)
        {
            // 새로운 배열을 생성 (첫 번째 요소는 0.0f, 나머지는 t[i] + 0.5f)
            float[] _move = new float[1+numProcesses];
            float[] _start = new float[1+numProcesses];
            float[] _finish = new float[1+numProcesses];
            _move[0] = 0.0f;  // 첫 번째 값은 0.0f
            _start[0] = 0.0f;  // 첫 번째 값은 0.0f
            _finish[0] = 0.0f;  // 첫 번째 값은 0.0f
        
            for (int i = 0; i < numProcesses; i++)
            {
                _move[i + 1] = t[i];
                _start[i + 1] = t[i] + 0.5f;  // t[i] + 0.5f를 result의 두 번째부터 채움
            }

            move.Add(_move);
            start.Add(_start);
            List<float> rnd = new List<float>();
            for (int u = 0; u < numProcesses; u++)
            {
                rnd.Add(UnityEngine.Random.Range(1.0f, 1.5f));
            }
            for (int i = 0; i < numProcesses; i++)
            {
                _finish[i + 1] = t[i] + rnd[i];
            }
            finish.Add(_finish);
            for (int u = 0; u < numProcesses; u++)
            {
                t[u] += interval;
            }
            
        }
            
        // 각각의 Job 객체 생성
        for (int d = 0; d < numBlocks; d++)
        {
            int coloridx = d % colorData.Count;
            jobs[d] = new Job(colorData[coloridx], d, BlockPrefab,
            positionData, source[d], sink[d],
            move[d], finish[d], start[d]);
        }

            

        ////------------------------------------------------
        //float[] move2 = new float[] { 0.0f, 4.0f, 7.0f, 9.0f,};
        //float[] start2 = new float[] { 0.0f, 4.5f, 7.5f, 9.5f};
        //float[] finish2 = new float[] { 0.0f, 7.0f, 9.0f, 11.0f};
        //jobs[1] = new Job(colorData[1], 1, BlockPrefab,
        //    positionData, source[1], sink[1],
        //    move2, finish2, start2);
        ////------------------------------------------------
        
        //float[] move3 = new float[] { 0.0f, 7.0f, 9.0f, 11.0f};
        //float[] start3 = new float[] { 0.0f, 7.5f, 9.5f, 11.5f};
        //float[] finish3 = new float[] { 0.0f, 8.0f, 10.0f, 13.0f};

        //jobs[2] = new Job(colorData[2], 2, BlockPrefab, 
        //    positionData, source[2], sink[2],
        //    move3, finish3, start3);

        //------------------------------------------------
        for (int j = 0; j < numProcesses; j++)
        {
            Debug.Log(j);
            machineposition = new Vector3(positionData[j+1].x, -0.31f, positionData[j+1].z);
            machines[j] = new Machine(ProcessPrefab, machineposition);
            Debug.Log("New machine " + j + " generated on " + machineposition);
        }




    }

    // Update is called once per frame
    void Update()
    {
        
        check_termination();

    }

    void check_termination()
    {

        int count = 0;
        for (int i = 0; i < jobs.Length; i++)
        {
            if (jobs[i].block.GetComponent<Block>().isFinished == true)
            {
                count++;
            }
        }

        if (count == jobs.Length)
        {
            Debug.Log("All Jobs Finished!");
        }
    }
    // CSV 파일에서 RGB 값을 읽어와서 List<float[3]> 형태로 저장
    List<Vector3> ReadPositionFromCSV(string file)
    {
        List<Vector3> positions = new List<Vector3>();  // 색상 리스트
        string path = Path.Combine(Application.dataPath, file);  // 파일 경로
        positions.Add(new Vector3(0.0f, 0.0f, 0.0f));  // float[3]로 저장
        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines)
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                float x = float.Parse(values[0], CultureInfo.InvariantCulture);  // Red
                float y = float.Parse(values[1], CultureInfo.InvariantCulture);  // Green
                float z = float.Parse(values[2], CultureInfo.InvariantCulture);  // Blue

                positions.Add(new Vector3(x, y, z));  // float[3]로 저장
            }
        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return positions;
    }

    List<(int idx, int machine, float move, float start, float finish)> ReadTimeTableFromCSV(string file)
    {
        var log = new List<(int idx, int machine, float move, float start, float finish)>();

        string path = Path.Combine(Application.dataPath, file);  // 파일 경로

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines.Skip(1))
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                int idx = int.Parse(values[0], CultureInfo.InvariantCulture);
                int machine = int.Parse(values[1], CultureInfo.InvariantCulture);
                float move = float.Parse(values[2], CultureInfo.InvariantCulture);
                float start = move + movingtime;
                float finish = float.Parse(values[3], CultureInfo.InvariantCulture);

                // ValueTuple을 리스트에 추가
                log.Add((idx, machine, move, start, finish));
            }

        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return log;

    }
    // CSV 파일에서 RGB 값을 읽어와서 List<float[3]> 형태로 저장
    List<Color> ReadColorsFromCSV(string file)
    {
        List<Color> colors = new List<Color>();  // 색상 리스트
        string path = Path.Combine(Application.dataPath, file);  // 파일 경로

        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);  // 모든 라인을 읽음

            foreach (string line in lines)
            {
                string[] values = line.Split(',');  // 쉼표로 값들을 분리
                float r = float.Parse(values[1], CultureInfo.InvariantCulture);  // Red
                float g = float.Parse(values[2], CultureInfo.InvariantCulture);  // Green
                float b = float.Parse(values[3], CultureInfo.InvariantCulture);  // Blue

                colors.Add(new Color(r / 255f, g / 255f, b / 255f));  // float[3]로 저장
            }
        }
        else
        {
            Debug.LogError("CSV file not found at: " + path);
        }

        return colors;
    }

    List<Vector3> stackPositions(int n_row, float startX, float startY, float startZ, float zOffset, float xOffset)
    {
        
        List<Vector3> sources = new List<Vector3>();
        // 처음 3개의 블록은 z 방향으로 쌓고, 그 이후로는 x 방향으로 나가면서 z 방향으로 쌓음
        int a;
        int b;
        for (int i = 0; i < numBlocks; i++)
        {
            a = i / n_row;
            b = i % n_row;
            sources.Add(new Vector3(startX + xOffset * a, startY, startZ + zOffset * b));
            
        }
        return sources;
    }
}
