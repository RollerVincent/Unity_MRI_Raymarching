using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Series : MonoBehaviour
{
    public string id;
    public Vector3 spacing;
    public Vector3Int shape;
    public float[] zLocs;
    [Range(0f,1f)]
    public float bottomCutoff;
    [Range(0f,1f)]
    public float topCutoff;
    [Range(0f,1f)]
    public float segmentTreshold;
    
    public float[,,] data;
    
    public Texture3D volumeTexture;

    public bool createVolumeTexture;
    public bool createPathTexture;
    public bool crunchVolumeTexture;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*Material mat = Resources.Load("Materials/Preview", typeof(Material)) as Material;
        mat.SetFloat("_BottomCutoff", bottomCutoff);
        mat.SetFloat("_TopCutoff", topCutoff);
        mat.SetFloat("_Segments", segmentTreshold);*/


        if(createVolumeTexture){
            createVolumeTexture = false;
            CreateVolumeTexture();
        }
        if(crunchVolumeTexture){
            crunchVolumeTexture = false;
            CrunchVolume();
        }
        if(createPathTexture){
            createPathTexture = false;
            CreatePathTexture();
        }

    }

    public void CheckLoad(){
        if(data == null){
            Load();
        }
    }

    public void Load(){
        if (File.Exists(Application.persistentDataPath + "/"+id+".serialized"))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/"+id+".serialized", FileMode.Open);
			SeriesSaveObject save = (SeriesSaveObject)bf.Deserialize(file);
			file.Close();

            data = save.data;
            spacing = new Vector3(save.spacing[0], save.spacing[1], save.spacing[2]);
            shape = new Vector3Int(save.shape[0], save.shape[1], save.shape[2]);
            zLocs = save.zLocs;
            
            Debug.Log("Loaded data");
			
		}
		else
		{
			Debug.Log("SAVE NOT FOUND");
		}
    }

    void UpdateVolumeMaterial(){
        /*Material mat = Resources.Load("Materials/Volume", typeof(Material)) as Material;
        mat.SetTexture("_MainTex", volumeTexture);
        mat.SetVector("_Shape", new Vector4(shape.x, shape.y, shape.z, 0));
        mat.SetVector("_Spacing", spacing);*/
        Master master = GameObject.Find("Main Camera").GetComponent<Master>();
        master.volumeTexture = volumeTexture;
        master.shape = shape;
        master.spacing = spacing;
        //master.ApplyVolumeTexture();
    }

    void CreateVolumeTexture(){
        CheckLoad();
        volumeTexture = new Texture3D (shape.x, shape.y, shape.z, TextureFormat.RHalf, true);   
        for (int x = 1; x < shape.x-1; x++)
        {
            for (int y = 1; y < shape.y-1; y++)
            {
                for (int z = 1; z < shape.z-1; z++)
                {
                    volumeTexture.SetPixel(x,y,z,new Color(data[x,y,z],0,0));
                }

            }
        }
        volumeTexture.Apply ();
        Debug.Log("Created volume texture");
        UpdateVolumeMaterial();
       
    }

    void CreatePathTexture(){
        CheckLoad();
        Texture3D pathTexture = new Texture3D (shape.x, shape.y, shape.z, TextureFormat.RHalf, true);

        bool[,,] mask = new bool[shape.x, shape.y, shape.z];
        float voxelCount = shape.x * shape.y * shape.z;

        Stack<Vector3Int> pivotStack = new Stack<Vector3Int>();
        

        Vector3Int pivot = new Vector3Int(0,0,0);
        int pathIndex = 0;
        pivotStack.Push(pivot);

        while(pivotStack.Count > 0){

            if(mask[pivot.x, pivot.y, pivot.z] == false){
                pathTexture.SetPixel(pivot.x, pivot.y, pivot.z, new Color(pathIndex/voxelCount, 0, 0));
                mask[pivot.x, pivot.y, pivot.z] = true;
                pathIndex += 1;
            } 


            float m = 1;
            Vector3Int smallestNeighbour = Vector3Int.zero;

            for (int x = -1; x < 2; x++){
                for (int y = -1; y < 2; y++){
                    for (int z = -1; z < 2; z++){
                        Vector3Int p = new Vector3Int(pivot.x+x, pivot.y+y, pivot.z+z);
                        if(p.x >= 0 && p.y >= 0 && p.z >= 0  &&  p.x < shape.x && p.y < shape.y && p.z < shape.z){
                            if(mask[p.x,p.y,p.z] == false){
                                if(data[p.x,p.y,p.z] < m){
                                    m = data[p.x,p.y,p.z];
                                    smallestNeighbour = p;
                                }
                                
                            }
                        }
                    }
                }
            }
            
            if(smallestNeighbour == Vector3Int.zero){
                pivot = pivotStack.Pop();
            }else{
                pivotStack.Push(pivot);
                pivot = smallestNeighbour;
                
            }



            


        }

        Debug.Log(pathIndex/voxelCount);

        pathTexture.Apply ();
        volumeTexture = pathTexture;
        Debug.Log("Created path texture");
        UpdateVolumeMaterial();
        
    }

    void CrunchVolume(){
        Texture3D tex = new Texture3D (shape.x/2, shape.y/2, shape.z/2, TextureFormat.RHalf, true);   
        for (int x = 0; x < shape.x/2; x++)
        {
            for (int y = 0; y < shape.y/2; y++)
            {
                for (int z = 0; z < shape.z/2; z++)
                {
                    Color c = Color.Lerp(volumeTexture.GetPixel(x*2+0, y*2+0, z*2+0),volumeTexture.GetPixel(x*2+1, y*2+1, z*2+1),0.5f);
                    tex.SetPixel(x,y,z,c);
                }
            }
        }
        tex.Apply ();
        volumeTexture = tex;
        
        shape = new Vector3Int(shape.x/2, shape.y/2, shape.z/2);
        spacing = spacing * 2;
        
        Debug.Log("Crunched volume texture");

        UpdateVolumeMaterial();
    }

  

    Color ToHP(float v){
        Color c = new Color(0,0,0);
        float d = 1f/5f;
        if(v>d*1){
            c.r = 1f;

            if(v>d*2){
                c.g = 1f;

                if(v>d*3){
                    c.b = 1f;

                    if(v>d*4){
                        c.r = 0f;
                        if(v>d*5){
                            c.g = 0f;
                            
                        }else{
                            c.g = 1f-(v % d)/d;
                        }
                    }else{
                        c.r = 1f-(v % d)/d;
                    }


                }else{
                    c.b = (v % d)/d;
                }

            }else{
                c.g = (v % d)/d;
            }

        }else{
            c.r = (v % d)/d;
        }
        return c;
        
        
    }

    Color ToHP2(float v){
        Color c = new Color(0,0,0);
        float d = 1f/3f;
        if(v>d*1){
            c.r = 1f;

            if(v>d*2){
                c.g = 1f;

                if(v>d*3){
                    c.b = 1f;
                }else{
                    c.b = (v % d)/d;
                }

            }else{
                c.g = (v % d)/d;
            }

        }else{
            c.r = (v % d)/d;
        }
        return c;
        
        
    }

   
 
}
