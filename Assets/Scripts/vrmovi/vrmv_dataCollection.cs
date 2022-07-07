using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltBrush;

public class vrmv_dataCollection : MonoBehaviour
{
    // member variables
    bool b_exported;
    string fbx_filePath;



    // Start is called before the first frame update
    void Start()
    {
        b_exported = false;
        fbx_filePath = Application.dataPath + "/Models/vrmovi/" + "test.fbx";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !b_exported)
        {
            ExportFbx.Export(fbx_filePath, ExportFbx.kFbxBinary);
            b_exported = true;
            Debug.Log("vrmv_dataCollection: Sketch model exported!");
        }
    }
}
