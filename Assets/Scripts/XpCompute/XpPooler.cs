using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComputeShaderUtility;
using UnityEngine.Rendering;
using System.Linq;

public class XpPooler : MonoBehaviour
{
    public static XpPooler i;
    public ComputeShader dustCompute;
    public int totalParticles = 1_000_000;
    public int currentParticles = 0;
    public Material instancedMaterial;
    public Mesh mesh;
    public float size;
    public Transform vacuumMouth;
    public float attractRadius = 1;
    public float attractForce = 10;
    public float eatDst = 0.2f;
    public bool updateProgress;
    public int toSpawn = 1000;
    public float spawnRad = 10f;

    ComputeBuffer particleBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorTypeBuffer;
    //ComputeBuffer numParticlesGeneratedBuffer;
    ComputeBuffer numParticlesConsumedBuffer;
    ComputeBuffer argsBuffer;

    const int InitDustKernel = 0;
    const int UpdateDustKernel = 1;
    AsyncGPUReadbackRequest readbackRequest;

    private void Awake()
    {
        i = this;
    }
    void Start()
    {
        ComputeHelper.CreateStructuredBuffer<Particle>(ref particleBuffer, totalParticles);
        ComputeHelper.CreateStructuredBuffer<Vector3>(ref positionBuffer, totalParticles);
        ComputeHelper.CreateStructuredBuffer<uint>(ref colorTypeBuffer, totalParticles);

        // Init dust particle positions
        dustCompute.SetBuffer(InitDustKernel, "particles", particleBuffer);
        dustCompute.SetBuffer(InitDustKernel, "positions", positionBuffer);
        dustCompute.SetBuffer(InitDustKernel, "colorTypes", colorTypeBuffer);
        dustCompute.SetInt("numParticles", totalParticles);
        dustCompute.SetFloat("size", size);


        // Create args buffer
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)totalParticles;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        args[4] = 0; // offset

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        dustCompute.SetBuffer(UpdateDustKernel, "particles", particleBuffer);
        dustCompute.SetBuffer(UpdateDustKernel, "positions", positionBuffer);
        ComputeHelper.CreateStructuredBuffer<uint>(ref numParticlesConsumedBuffer, 4);
        numParticlesConsumedBuffer.SetData(new uint[] { 0, 0, 0, 0 });
        dustCompute.SetBuffer(UpdateDustKernel, "numParticlesConsumed", numParticlesConsumedBuffer);

        RequestAsyncReadback();

        instancedMaterial.SetBuffer("positionBuffer", positionBuffer);
        instancedMaterial.SetBuffer("colorTypeBuffer", colorTypeBuffer);
    }

    void RequestAsyncReadback()
    {
        readbackRequest = AsyncGPUReadback.Request(numParticlesConsumedBuffer);
    }
    public void SpawnXp(int numPoints, Vector2 pos, Element element)
    {
        dustCompute.SetInt("currPointCount", currentParticles % totalParticles);
        dustCompute.SetInt("numPoints2Add", numPoints);
        dustCompute.SetInt("colorType2Add", (int)element);
        dustCompute.SetVector("spawnCenter", pos);
        instancedMaterial.color = AssetDB.i.elementAffinity[element].colourProfile;
        ComputeHelper.Dispatch(dustCompute, totalParticles, 1, 1, InitDustKernel);
        currentParticles += numPoints;
    }

    void Update()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, instancedMaterial, new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffer);
        if (GameManager.IsPaused) return;
        dustCompute.SetFloat("deltaTime", Time.deltaTime);
        dustCompute.SetVector("attractorPos", transform.position);
        dustCompute.SetInt("numParticles", totalParticles);
        dustCompute.SetFloat("size", size);
        dustCompute.SetVector("attractPos", vacuumMouth.position);
        dustCompute.SetFloat("attractRadius", attractRadius);
        dustCompute.SetFloat("eatDst", eatDst);
        dustCompute.SetFloat("attractForce", attractForce);
        dustCompute.SetFloat("spawnRad", spawnRad);

        ComputeHelper.Dispatch(dustCompute, totalParticles, 1, 1, UpdateDustKernel);


        if (updateProgress && readbackRequest.done)
        {
            var n = readbackRequest.GetData<uint>().ToArray();
            // This only goes up to uint max, use a timer to update in player script to check for xp changes
            print($"partcletypes consumed: {n[0]}, {n[1]}, {n[2]}, {n[3]}");
            //Debug.Log(n + " / " + totalParticles + "  " + (n / (float)totalParticles) * 100 + "%");
            RequestAsyncReadback();
        }
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SpawnXp(toSpawn, GameManager.Player.transform.position , Element.Water);
        }
#endif
    }

    void OnDestroy()
    {
        ComputeHelper.Release(particleBuffer, positionBuffer, argsBuffer, numParticlesConsumedBuffer);
    }

    public struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public float alpha;
        public Element element;
    }
}
