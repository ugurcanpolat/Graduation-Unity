using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using Newtonsoft.Json;

public class MainController : MonoBehaviour
{
    // Base URL for web scrapping
    private const string BASE_URL = "http://ugurcans-macbook-pro.local:8080/";

    public GameObject refreshButtonObject;
    public GameObject inputButtonObject;
    public GameObject modeDropdownObject;
    public GameObject inputDropdownObject;
    public GameObject inputFieldObject;

    public List<GameObject> textObjects;
    public List<GameObject> imageObjects;
    public List<GameObject> pieChartObjects;

    private Dropdown modeDropdown;
    private Dropdown inputDropdown;
    private InputField inputField;

    private ChartResponse chartResponse;

    private bool repeaterActive = false;

    // Start is called before the first frame update
    void Start()
    {
        MakeAllObjectsHidden();

        modeDropdown = modeDropdownObject.GetComponent<Dropdown>();
        inputDropdown = inputDropdownObject.GetComponent<Dropdown>();
        inputField = inputFieldObject.GetComponent<InputField>();
    }

    private void MakeAllObjectsHidden()
    {
        for (int i = 0; i < 4; i++)
        {
            textObjects[i].SetActive(false);
            imageObjects[i].SetActive(false);
            pieChartObjects[i].SetActive(false);

            textObjects[i].transform.parent.gameObject.SetActive(false);
        }
        inputButtonObject.SetActive(false);
    }

    public void ModeHasChanged()
    {
        repeaterActive = false;

        if (modeDropdown.value == 2)
        {
            refreshButtonObject.SetActive(false);
            RepeatCommunication();
            return;

        }

        refreshButtonObject.SetActive(true);
    }

    public void InputHasSelected()
    {
        Debug.Log("Input selection has changed.");
        inputField.text = null;

        int modifiableCount = -1;
        int index = 0;

        for (; index < chartResponse.data.Count; index++)
        {
            if (chartResponse.data[index].modifiable) modifiableCount++;
            if (modifiableCount == inputDropdown.value) break;
        }

        switch (chartResponse.data[index].dataType)
        {
            case "float":
                inputField.contentType = InputField.ContentType.DecimalNumber;
                break;
            case "integer":
                inputField.contentType = InputField.ContentType.IntegerNumber;
                break;
            default:
                inputField.contentType = InputField.ContentType.Standard;
                break;
        }
    }

    public void InputValueChanged()
    {
        Debug.Log("Input value has changed.");
        if (inputField.text == null) inputButtonObject.SetActive(false);
        else if (inputField.text.Length == 0) inputButtonObject.SetActive(false);
        else inputButtonObject.SetActive(true);
    }

    public void SendInputButtonClicked()
    {
        StartCoroutine("SendInputData");
    }

    private IEnumerator SendInputData()
    {
        string url = BASE_URL + "modifyData/";

        ModifyDataRequest changeDataRequest = new ModifyDataRequest
        {
            name = inputDropdown.captionText.text.ToLower(),
            operation = "add",
            value = System.Convert.ToSingle(inputField.text),
            index = 0
        };

        string jsonString = JsonConvert.SerializeObject(changeDataRequest);

        UnityWebRequest request = UnityWebRequest.Put(url, 
                            System.Text.Encoding.Default.GetBytes(jsonString));
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        if (!request.isNetworkError && request.responseCode == 200)
        {
            Debug.Log("Communicated: modifying server data.");
            Debug.Log(request.downloadHandler.text);

            string stringResponse = request.downloadHandler.text;
            ModifyDataResponse response = 
                JsonConvert.DeserializeObject<ModifyDataResponse>(
                                                            stringResponse);

            if (response.success)
            {
                Debug.Log("Success: modifying server data.");
            }
            else
            {
                Debug.Log("Fail: modifying server data.");
                Debug.Log(response.errorMsg);
            }
        }
        else
        {
            Debug.Log("Fail: modifying server data");
            Debug.Log(request.responseCode);
            Debug.Log(request.error);
        }
    }

    private Object ConvertStringToProperType(string value, string type)
    {
        return null;
    }

    public void RefreshButtonClicked()
    {
        Debug.Log("Refresh button is clicked.");

        if (modeDropdown.value == 1)
        {
            RepeatCommunication();
            return;
        }

        StartCoroutine("CommunicateWithServer");
    }

    private void RepeatCommunication()
    {
        Debug.Log("Repeat communication activated.");

        if (!repeaterActive)
        {
            repeaterActive = true;
        }
        else
        {
            repeaterActive = false;
            return;
        }

        StartCoroutine("Repeater");
    }

    private IEnumerator Repeater()
    {
        while (repeaterActive)
        {
            StartCoroutine("CommunicateWithServer");
            yield return new WaitForSeconds(5);
        }
    }

    private IEnumerator CommunicateWithServer()
    {
        string url = BASE_URL + "temperature/";

        ChartRequest chartRequest = new ChartRequest();
        string jsonString = JsonUtility.ToJson(chartRequest);

        UnityWebRequest request = UnityWebRequest.Post(url, jsonString);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        if (!request.isNetworkError && request.responseCode == 200)
        {
            Debug.Log("Success.");
            Debug.Log(request.downloadHandler.text);

            string stringResponse = request.downloadHandler.text;

            chartResponse = JsonConvert.DeserializeObject<ChartResponse>(
                                                            stringResponse);
        }
        else
        {
            Debug.Log("Fail");
            Debug.Log(request.responseCode);
            Debug.Log(request.error);

            chartResponse = new ChartResponse
            {
                success = false,
                errorMsg = request.error,
            };
        }
        UpdateUI();
    }

    private IEnumerator GetImage(string URL, int imageLocation)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(URL);

        using (request)
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(request);

                Rect rect = new Rect(0, 0, texture.width, texture.height);
                Vector2 vect = new Vector2(0.5f, 0.5f);
                Sprite sprite = Sprite.Create(texture, rect, vect, 100);

                Image image = imageObjects[imageLocation].GetComponent<Image>();
                image.rectTransform.sizeDelta = new Vector2(texture.width*4, texture.height*4);
                image.sprite = sprite;
                imageObjects[imageLocation].SetActive(true);
                imageObjects[imageLocation].transform.parent.gameObject.SetActive(true);
            }
        }
    }

    private void UpdateUI()
    {
        MakeAllObjectsHidden();

        if (!chartResponse.success)
        {
            Debug.Log("Fail to update UI.");
            textObjects[0].GetComponent<Text>().text = chartResponse.errorMsg;
            textObjects[0].SetActive(true);
            textObjects[0].transform.parent.gameObject.SetActive(true);
            return;
        }

        foreach (DataModel data in chartResponse.data)
        {
            RepresentData(data);
        }

        UpdateInputDropdownAndInputField();
    }

    private void UpdateInputDropdownAndInputField()
    {
        switch (chartResponse.data[0].dataType)
        {
            case "float":
                inputField.contentType = InputField.ContentType.DecimalNumber;
                break;
            case "integer":
                inputField.contentType = InputField.ContentType.IntegerNumber;
                break;
            default:
                inputField.contentType = InputField.ContentType.Standard;
                break;
        }

        List<Dropdown.OptionData> inputOptionDatas = new List<Dropdown.OptionData>();

        for (int i = 0; i < chartResponse.data.Count; i++)
        {
            if (!chartResponse.data[i].modifiable) continue;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (char c in chartResponse.data[i].name)
            {
                if (char.IsUpper(c) && builder.Length > 0) builder.Append(' ');
                builder.Append(c);
            }

            string optionText = builder.ToString();
            optionText = optionText.Substring(0, 1).ToUpper() + optionText.Substring(1);

            Dropdown.OptionData option = new Dropdown.OptionData
            {
                text = optionText
            };
            inputOptionDatas.Add(option);
        }

        inputDropdown.ClearOptions();
        inputDropdown.AddOptions(inputOptionDatas);
    }

    private void RepresentData(DataModel data)
    {
        List<int> intValues = null;
        List<float> floatValues = null;
        List<string> stringValues = null;

        switch (data.dataType)
        {
            case "integer":
                intValues = data.values.ConvertAll<int>(
                           new System.Converter<object, int>(ObjectToInt));
                break;
            case "float":
                floatValues = data.values.ConvertAll<float>(
                           new System.Converter<object, float>(ObjectToFloat));
                break;
            default:
                stringValues = data.values.ConvertAll<string>(
                           new System.Converter<object, string>(ObjectToString));
                break;
        }

        switch (data.visual)
        {
            case "text":
                if (stringValues != null)
                {   
                    CreateTextObject(stringValues[0], data.screenLocation - 1);
                }
                break;
            case "image":
                if (stringValues != null)
                {
                    CreateImageObject(stringValues[0], data.screenLocation - 1);
                }
                break;
            case "pie":
                if (floatValues != null)
                {
                    CreatePieChartObject(floatValues, data.screenLocation - 1);
                }
                break;
        }
    }

    private void CreateTextObject(string text, int screenLocation)
    {
        textObjects[screenLocation].GetComponent<Text>().text = text;
        textObjects[screenLocation].SetActive(true);
        textObjects[screenLocation].transform.parent.gameObject.SetActive(true);
    }

    private void CreateImageObject(string imageLink, int screenLocation)
    {
        StartCoroutine(GetImage(imageLink, screenLocation));
    }

    private void CreatePieChartObject(List<float> values, int screenLocation)
    {
        List<Color> colors = new List<Color>();
        for (int i = 0; i < values.Count; i++)
        {
            colors.Add(new Color(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)));
        }

        foreach (Transform child in pieChartObjects[screenLocation].transform)
        {
            Destroy(child.gameObject);
        }

        PieChart pieChart = pieChartObjects[screenLocation].GetComponent<PieChart>();

        pieChart.values = values;

        pieChart.wedgeColors = colors;
        pieChart.MakePieChart();
        pieChartObjects[screenLocation].SetActive(true);
        pieChartObjects[screenLocation].transform.parent.gameObject.SetActive(true);
    }

    private int ObjectToInt(object item)
    {
        return System.Convert.ToInt32(item);
    }

    private float ObjectToFloat(object item)
    {
        return System.Convert.ToSingle(item);
    }

    private string ObjectToString(object item)
    {
        return System.Convert.ToString(item);
    }

}
