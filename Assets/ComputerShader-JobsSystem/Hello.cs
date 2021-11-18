//#define USE_GPU 
#define USE_JOBS

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


struct Bird
{
    public float speed;
    public Vector3 position;
    public Vector3 direction;
    public Bird(Vector3 pos, Vector3 dir, float s)
    {
        position = pos;
        direction = dir;
        speed = s;
    }
};

#if USE_JOBS
struct BirdJob : IJobParallelFor
{
    [ReadOnly]
    public Vector3 target;
    [ReadOnly]
    public float neighbour;
    [ReadOnly]
    public float deltaTime;
    [ReadOnly]
    public NativeArray<Bird> bird_data;
    public NativeArray<Bird> bird_out_data;
    public void Execute(int idx)
    {
        Bird b = bird_out_data[idx];
        Vector3 spearation = Vector3.zero;
        Vector3 alighment = Vector3.zero;
        Vector3 cohesion = target;
        int n = 1;
        for (int i = 0; i < bird_data.Length; i++) {
            var x = bird_data[i];
            Vector3 dir = b.position - x.position;
            float dist = Mathf.Max(dir.magnitude, 0.000001f);
            if (dist < neighbour) {
                spearation += dir * (float)(1.0 / dist - 1.0 / neighbour);
                alighment += x.direction;
                cohesion += x.position;
                n++;
            }
        }
        float avg = 1.0f / n;
        alighment *= avg;
        cohesion *= avg;
        cohesion = Vector3.Normalize(cohesion - b.position);
        Vector3 direction = Vector3.Normalize(alighment + spearation + cohesion);
        b.direction = Vector3.Lerp(direction, b.direction, 0.94f);
        b.position += b.direction * b.speed * deltaTime;
        bird_out_data[idx] = b;
    }
}
#endif

public class Hello : MonoBehaviour
{
    public float speed = 1f;
    public float neighbour = 1f;
    public int bird_count = 100;
    public float span_radius = 2f;
    public ComputeShader shader;
    public GameObject template;
    public GameObject Aim;

    private int kernel;
    private int group_size;
    private Bird[] bird_data;
    private BirdJob bird_job;
    private ComputeBuffer bird_gpu;
    private NativeArray<Bird>[] bird_jobs_data;
    private GameObject[] birds;
    private Vector3 target;

    private void InitBirds()
    {
        bird_data = new Bird[bird_count];
        birds = new GameObject[bird_count];
        for (int i = 0; i < bird_count; i++) { 
            var rnd = Random.insideUnitCircle;
            var pos = transform.position + new Vector3(rnd.x, 0.5f, rnd.y) * span_radius;
            birds[i] = Instantiate(template, pos, Quaternion.identity);
            birds[i].transform.transform.localRotation = Quaternion.AngleAxis(-90, Vector3.right);
            bird_data[i] = new Bird(pos, birds[i].transform.forward, speed);
        }
        target = Vector3.zero;
    }

    private void InitShader()
    {
        bird_gpu = new ComputeBuffer(bird_count, 7 * sizeof(float));
        bird_gpu.SetData(bird_data);
        shader.SetBuffer(kernel, "bird_data", bird_gpu);
        shader.SetFloat("speed", speed);
        shader.SetFloat("neighbour", neighbour);
        shader.SetInt("bird_count", bird_count);
        shader.SetVector("target", target);
    }

    private void InitJobs()
    {
        bird_job = new BirdJob();
        bird_jobs_data = new NativeArray<Bird>[2];
        bird_jobs_data[0] = new NativeArray<Bird>(bird_data, Allocator.Persistent);
        bird_jobs_data[1] = new NativeArray<Bird>(bird_data, Allocator.Persistent);
    }

    private void Awake()
    {
        target = Aim.transform.position;
#if USE_GPU
        uint x;
        kernel = shader.FindKernel("CSMain");
        shader.GetKernelThreadGroupSizes(kernel, out x, out _, out _);
        group_size = Mathf.CeilToInt(bird_count / (float)x);
        bird_count = (int)(group_size * x);
 #endif
        InitBirds();
#if USE_GPU
        InitShader();
#endif
#if USE_JOBS
        InitJobs();
#endif
    }

    void UpdateBird(ref Bird b)
    {
        Vector3 spearation = Vector3.zero;
        Vector3 alighment = Vector3.zero;
        Vector3 cohesion = target;
        int n = 1;
        for (int i = 0; i < bird_data.Length; i++) {
            var x = bird_data[i];
            Vector3 dir = b.position - x.position;
            float dist = Mathf.Max(dir.magnitude, 0.000001f);
            if (dist < neighbour) {
                spearation += dir * (float)(1.0 / dist - 1.0 / neighbour);
                alighment += x.direction;
                cohesion += x.position;
                n++;
            }
        }
        float avg = 1.0f / n;
        alighment *= avg;
        cohesion *= avg;
        cohesion = Vector3.Normalize(cohesion - b.position);
        Vector3 direction = Vector3.Normalize(alighment + spearation + cohesion);
        b.direction = Vector3.Lerp(direction, b.direction, 0.94f);
        b.position += b.direction * b.speed * Time.deltaTime;
    }

    void SyncTransform()
    {
        for (int i = 0; i < bird_count; i++) {
            birds[i].transform.localPosition = bird_data[i].position;
            if (!bird_data[i].direction.Equals(Quaternion.identity)) {
                birds[i].transform.localRotation = Quaternion.LookRotation(bird_data[i].direction);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        target = Aim.transform.position;
#if USE_GPU
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("target", target);
        shader.Dispatch(kernel, group_size, 1, 1);
        bird_gpu.GetData(bird_data);
        SyncTransform();
#elif USE_JOBS
        bird_jobs_data[0].CopyFrom(bird_data);
        bird_job.target = target;
        bird_job.neighbour = neighbour;
        bird_job.target = target;
        bird_job.deltaTime = Time.deltaTime;
        bird_job.bird_data = bird_jobs_data[0];
        bird_job.bird_out_data = bird_jobs_data[1];
        var handle = bird_job.Schedule(bird_count, 1);
        handle.Complete();
        var a = bird_jobs_data[0];
        var b = bird_jobs_data[1];
        /*
        bird_jobs_data[0] = b;
        bird_jobs_data[1] = a;
        */
        b.CopyTo(bird_data);
        for (int i = 0; i < bird_count; i++) {
            Bird bx = bird_data[i];
            birds[i].transform.localPosition = bx.position;
            if (!bx.direction.Equals(Vector3.zero)) {
                birds[i].transform.localRotation = Quaternion.LookRotation(bx.direction);
            }
        }
#else
        for (int i = 0; i < bird_count; i++) {
            UpdateBird(ref bird_data[i]);
        }
        SyncTransform();
#endif
    }
    private void OnDestroy()
    {
#if USE_GPU
        bird_gpu.Dispose();
#endif
#if USE_JOBS
        bird_jobs_data[0].Dispose();
        bird_jobs_data[1].Dispose();
#endif
    }
}
