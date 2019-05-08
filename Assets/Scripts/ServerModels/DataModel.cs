using System.Collections.Generic;

[System.Serializable]
public class DataModel
{
    public string name;
    public string visual;
    public int screenLocation;
    public string dataType;
    public bool modifiable;
    public List<object> values;
}
