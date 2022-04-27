using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[ExecuteInEditMode]
public class Viewer : MonoBehaviour
{

    public string seriesPath;
    public bool import;
    public bool save;
    public bool load;
    public bool create;

    float[,,] data;
    public Vector3 spacing;
    public Vector3Int shape;
    public float[] zLocs;

    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(import){
            import = false;
            ImportSeries();
        }
        if(save){
            save = false;
            SaveSeries();
        }
        if(load){
            load = false;
            LoadSeries();
        }
        if(create){
            create = false;
            CreateSeries();
        }
    }

    

    void ImportSeries(){
        data = null;
        spacing = Vector3.zero;
        shape = Vector3Int.zero;

        DirectoryInfo directoryInfo = new DirectoryInfo("Assets/" + seriesPath);

        foreach (FileInfo file in directoryInfo.GetFiles()){
            if(file.Name.EndsWith("metadata.txt")){
                Debug.Log(file.FullName);
                StreamReader inp_stm = new StreamReader(file.FullName);
                while(!inp_stm.EndOfStream)
                {
                    string line = inp_stm.ReadLine();
                    string[] s = line.Split("\t");
                    spacing = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));

                    line = inp_stm.ReadLine();
                    s = line.Split("\t");
                    shape = new Vector3Int(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));

                    line = inp_stm.ReadLine();
                    s = line.Split("\t");
                    zLocs = new float[s.Length-1];
                    for(int i=0;i<s.Length-1;i++){
                        zLocs[i] = float.Parse(s[i]);
                    }
                    
                }
                inp_stm.Close();  
            }
        }

        data = new float[shape.x,shape.y,shape.z];

        foreach (FileInfo file in directoryInfo.GetFiles()){
            if(file.Name.EndsWith(".tsv")){
                Debug.Log(file.Name);
                int z = int.Parse(file.Name.Split('.')[0]);
                StreamReader inp_stm = new StreamReader(file.FullName);
                int y = 0;
                while(!inp_stm.EndOfStream)
                {
                    string line = inp_stm.ReadLine();
                    string[] s = line.Split("\t");
                    for(int x=0; x<s.Length; x++){
                        float v = float.Parse(s[x]);
                        //Debug.Log(x + " " + y + " " + z);
                        data[y,x,z] = v;
                    }
                    


                    y += 1;
                }
                inp_stm.Close();  
            }  
        }


    }

    public void SaveSeries(){

        SeriesSaveObject save = new SeriesSaveObject();

        save.data = data;
        save.spacing = new float[]{spacing.x, spacing.y, spacing.z};
        save.shape = new int[]{shape.x, shape.y, shape.z};
        save.zLocs = zLocs;

        BinaryFormatter bf;
        FileStream file;
		bf = new BinaryFormatter();
		file = File.Create(Application.persistentDataPath + "/"+seriesPath+".serialized");
		bf.Serialize(file, save);
		file.Close();

    }

    public void LoadSeries(){
        if (File.Exists(Application.persistentDataPath + "/"+seriesPath+".serialized"))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/"+seriesPath+".serialized", FileMode.Open);
			SeriesSaveObject save = (SeriesSaveObject)bf.Deserialize(file);
			file.Close();

            data = save.data;
            spacing = new Vector3(save.spacing[0], save.spacing[1], save.spacing[2]);
            shape = new Vector3Int(save.shape[0], save.shape[1], save.shape[2]);
            zLocs = save.zLocs;
            
			
		}
		else
		{
			Debug.Log("SAVE NOT FOUND");
		}
    }

    void CreateSeries(){
        GameObject newObj = new GameObject(seriesPath);
        Series series = newObj.AddComponent<Series>();

        series.data = data;
        series.spacing = spacing;
        series.shape = shape;
        series.zLocs = zLocs;
        series.id = seriesPath;

    }

    
}

[System.Serializable]
public class SeriesSaveObject
{
  public float[,,] data;
  public float[] spacing;
  public int[] shape;
  public float[] zLocs;

}
