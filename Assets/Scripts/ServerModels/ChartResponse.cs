using System.Collections.Generic;

[System.Serializable]
public class ChartResponse
{
    public bool success;
    public string errorMsg;
    public List<DataModel> data;

}
