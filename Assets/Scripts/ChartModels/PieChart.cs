using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieChart : MonoBehaviour
{
    public Image wedgePrefab;
    public List<Color> wedgeColors;
    public List<float> values; 

    public void MakePieChart()
    {
        float total = 0f;
        float zRotation = 0f;

        for (int i = 0; i < values.Count; i++)
        {
            total += values[i];
        }

        for (int i = 0; i < values.Count; i++)
        {
            Image newWedge = Instantiate (wedgePrefab) as Image;
            newWedge.transform.SetParent(transform, false);
            newWedge.color = wedgeColors[i];
            newWedge.fillAmount = values[i] / total;
            newWedge.transform.rotation = Quaternion.Euler(
                                            new Vector3(0f, 0f, zRotation));
            zRotation -= newWedge.fillAmount * 360f;
        }
    }

}
