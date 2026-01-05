using UnityEngine;

public class Rotation : MonoBehaviour
{
    [ContextMenu("X")]
    public void xRotation(){
        transform.Rotate(90,0,0,Space.World);

    }

    [ContextMenu("Y")]
    public void yRotation(){
        transform.Rotate(0,90,0,Space.World);

    }

    [ContextMenu("Z")]
    public void zRotation(){
        transform.Rotate(0,0,90,Space.World);

    }

}
