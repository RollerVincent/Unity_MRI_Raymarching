using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class Master : MonoBehaviour {
    public ComputeShader raymarching;
    public Vector3Int shape;
    public Vector3 spacing;
    public Texture3D volumeTexture;
    public Texture2D gradientTexture;
    public Vector3 cageDimensions;
    [Range(0.01f, 1f)]
    public float surfaceLow = 0.2f;
    [Range(0.00001f, 20f)]
    public float normalBias = 0.2f;
    [Range(0f, 0.1f)]
    public float occlusionDistance = 1f;
    [Range(0f, 10f)]
    public float occlusionIntensity = 1f;
    [Range(0f, 1f)]
    public float ambient = 1f;
    public Color fogColor;
    public float fogIntensity;
    public Color backgroundColor;
    [Range(0f, 1f)]
    public float segmentation = 1f;
    [Range(0f, 100f)]
    public float xRay = 1f;
    


    RenderTexture target;
    Camera cam;
    Light lightSource;
    List<ComputeBuffer> buffersToDispose;

    void Init () {
        cam = Camera.current;
        lightSource = FindObjectOfType<Light> ();
        if(volumeTexture == null){
            volumeTexture = new Texture3D(shape.x, shape.y, shape.z, TextureFormat.RFloat, true);
        }
        if(gradientTexture == null){
            gradientTexture = Texture2D.whiteTexture;
        }
        
    }

    public void ApplyVolumeTexture(){
        raymarching.SetTexture (0, "_VolumeTexture", volumeTexture);
        raymarching.SetVector ("_Shape", new Vector4(shape.x, shape.y, shape.z, 0));
        raymarching.SetVector ("_Spacing", new Vector4(spacing.x, spacing.y, spacing.z, 0));
    }


    void OnRenderImage (RenderTexture source, RenderTexture destination) {
        Init ();
        buffersToDispose = new List<ComputeBuffer> ();

        InitRenderTexture ();
        //CreateScene ();
        SetParameters ();
        //ApplyVolumeTexture();

        raymarching.SetTexture (0, "Source", source);
        raymarching.SetTexture (0, "Destination", target);

        int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
        int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
        raymarching.Dispatch (0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit (target, destination);

        foreach (var buffer in buffersToDispose) {
            buffer.Dispose ();
        }
    }

    void CreateScene () {
        

        /*ComputeBuffer shapeBuffer = new ComputeBuffer (shapeData.Length, ShapeData.GetSize ());
        shapeBuffer.SetData (shapeData);
        raymarching.SetBuffer (0, "shapes", shapeBuffer);
        raymarching.SetInt ("numShapes", shapeData.Length);

        buffersToDispose.Add (shapeBuffer);*/
    }

    void SetParameters () {
        bool lightIsDirectional = lightSource.type == LightType.Directional;
        raymarching.SetMatrix ("_CameraToWorld", cam.cameraToWorldMatrix);
        raymarching.SetMatrix ("_CameraInverseProjection", cam.projectionMatrix.inverse);
        raymarching.SetVector ("_Light", (lightIsDirectional) ? lightSource.transform.forward : lightSource.transform.position);
        raymarching.SetBool ("positionLight", !lightIsDirectional);
        raymarching.SetTexture (0, "_VolumeTexture", volumeTexture);
        raymarching.SetTexture (0, "_GradientTexture", gradientTexture);
        raymarching.SetVector ("_Shape", new Vector4(shape.x, shape.y, shape.z, 0));
        raymarching.SetVector ("_Spacing", new Vector4(spacing.x, spacing.y, spacing.z, 0));
        raymarching.SetFloat ("_SurfaceLow", surfaceLow);
        raymarching.SetFloat ("_NormalBias", normalBias);
        raymarching.SetFloat ("_OcclusionDistance", occlusionDistance);
        raymarching.SetFloat ("_OcclusionIntensity", occlusionIntensity);
        raymarching.SetFloat ("_Ambient", ambient);
        raymarching.SetFloat ("_FogIntensity", fogIntensity);
        raymarching.SetFloat ("_Segmentation", segmentation);
        raymarching.SetFloat ("_XRay", xRay);
        raymarching.SetFloats("_FogColor", new float[]{fogColor.r, fogColor.g, fogColor.b, fogColor.a});
        raymarching.SetFloats("_BackgroundColor", new float[]{backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a});
        raymarching.SetVector ("_CageDimensions", new Vector4(cageDimensions.x, cageDimensions.y, cageDimensions.z, 0));
        
    }

    void InitRenderTexture () {
        if (target == null || target.width != cam.pixelWidth || target.height != cam.pixelHeight) {
            if (target != null) {
                target.Release ();
            }
            target = new RenderTexture (cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create ();
        }
    }

}
