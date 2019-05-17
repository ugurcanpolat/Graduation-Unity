using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainController : MonoBehaviour
{
    // Base URL for web scrapping
    private const string BASE_URL = "http://ugurcans-macbook-pro.local:8080/";

    public GameObject refreshButtonObject;
    public GameObject inputButtonObject;
    public GameObject modeDropdownObject;
    public GameObject inputDropdownObject;
    public GameObject inputFieldObject;
    public GameObject inputTitleObject;
    public GameObject ControlPanelObject;
    public GameObject ControlButtonObject;

    private Animator controlPanelAnimator;
    private Animator controlButtonAnimator;

    public List<GameObject> textObjects;
    public List<GameObject> imageObjects;
    public List<GameObject> pieChartObjects;
    public List<GameObject> lineBarChartObjects;

    private List<Text> verticalTitleObjects;
    private List<Text> horizontalTitleObjects;

    private List<GameObject> gameObjectList;

    private List<RectTransform> lineBarChartTransforms;
    private List<RectTransform> labelTemplateX;
    private List<RectTransform> labelTemplateY;

    private List<string> modifiableNames; 

    [SerializeField] public Sprite circleSprite;

    private Dropdown modeDropdown;
    private Dropdown inputDropdown;
    private InputField inputField;

    private ChartResponse chartResponse;

    private string previousImageLink;

    private bool repeaterActive = false;

    // Start is called before the first frame update
    void Start()
    {
        MakeAllObjectsHidden();

        controlPanelAnimator = ControlPanelObject.GetComponent<Animator>();
        controlButtonAnimator = ControlButtonObject.GetComponent<Animator>();

        modifiableNames = new List<string>();

        lineBarChartTransforms = new List<RectTransform>();
        labelTemplateX = new List<RectTransform>();
        labelTemplateY = new List<RectTransform>();

        horizontalTitleObjects = new List<Text>();
        verticalTitleObjects = new List<Text>();

        gameObjectList = new List<GameObject>();

        for (int i = 0; i < 4; i++)
        {
            lineBarChartTransforms.Add(lineBarChartObjects[i].GetComponent<RectTransform>());
            labelTemplateX.Add(lineBarChartTransforms[i].Find("labelTemplateX")
                .GetComponent<RectTransform>());
            labelTemplateY.Add(lineBarChartTransforms[i].Find("labelTemplateY")
                .GetComponent<RectTransform>());
            horizontalTitleObjects.Add(lineBarChartTransforms[i].Find("horizontalTitle")
                .gameObject.GetComponent<Text>());
            verticalTitleObjects.Add(lineBarChartTransforms[i].Find("verticalTitle")
                .gameObject.GetComponent<Text>());
        }

        modeDropdown = modeDropdownObject.GetComponent<Dropdown>();
        inputDropdown = inputDropdownObject.GetComponent<Dropdown>();
        inputField = inputFieldObject.GetComponent<InputField>();
    }

    private GameObject CreateCircle(Vector2 anchoredPosition, int screenLocation)
    {
        GameObject dotObject = new GameObject("circle", typeof(Image));
        dotObject.transform.SetParent(lineBarChartTransforms[screenLocation], false);
        dotObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rectTransform = dotObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(11,11);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        return dotObject;
    }

    private void CreateLineChartObject(List<float> values, int screenLocation)
    {
        Debug.Log("Creating line chart.");

        foreach(GameObject chartObject in gameObjectList)
        {
            Destroy(chartObject);
        }
        gameObjectList.Clear();


        float graphHeight = lineBarChartTransforms[screenLocation].sizeDelta.y;
        float graphWidth = lineBarChartTransforms[screenLocation].sizeDelta.x;

        int maxVisibleValueAmount = 10;

        float yMax = values[0];
        float yMin = values[0];

        for (int i = Mathf.Max(values.Count - maxVisibleValueAmount, 0); i < values.Count; i++)
        {
            float value = values[i];

            if (value > yMax)
                yMax = value;

            if (value < yMin)
                yMin = value;
        }

        float yDifference = yMax - yMin;

        if (yDifference < .5f && yDifference > 0f)
        {
            yDifference = 5f;
        }

        yMax += yDifference * .2f;
        yMin -= yDifference * .2f;

        float xSize = graphWidth / (maxVisibleValueAmount + 1);

        int xIndex = 0;

        GameObject lastCircleObject = null;
        for (int i = Mathf.Max(values.Count - maxVisibleValueAmount, 0); i < values.Count; i++)
        {
            float xPos = xSize + xIndex * xSize;
            float yPos = (values[i] - yMin) / (yMax - yMin) * graphHeight;
            GameObject circleObject = CreateCircle(new Vector2(xPos, yPos), screenLocation);
            gameObjectList.Add(circleObject);

            if (lastCircleObject != null)
            {
                GameObject dotConnection = CreateDotConnection(
                    lastCircleObject.GetComponent<RectTransform>().anchoredPosition,
                    circleObject.GetComponent<RectTransform>().anchoredPosition,
                    screenLocation);
                gameObjectList.Add(dotConnection);
            }
            lastCircleObject = circleObject;

            RectTransform labelX = Instantiate(labelTemplateX[screenLocation]);
            labelX.SetParent(lineBarChartTransforms[screenLocation]);
            labelX.gameObject.SetActive(true);
            labelX.anchoredPosition = new Vector2(xPos, -3f);
            labelX.GetComponent<Text>().text = (i+1).ToString();
            gameObjectList.Add(labelX.gameObject);

            xIndex++;
        }

        int seperatorCount = 5;
        for (int i = 0; i <= seperatorCount; i++)
        {
            RectTransform labelY = Instantiate(labelTemplateY[screenLocation]);
            labelY.SetParent(lineBarChartTransforms[screenLocation]);
            labelY.gameObject.SetActive(true);
            float normalizedValue = i * 1f / seperatorCount;
            labelY.anchoredPosition = new Vector2(-3f, normalizedValue * graphHeight);
            labelY.GetComponent<Text>().text = (yMin + normalizedValue * (yMax - yMin))
                .ToString("0.0");
            gameObjectList.Add(labelY.gameObject);
        }

        lineBarChartObjects[screenLocation].SetActive(true);
        lineBarChartObjects[screenLocation].transform.parent.gameObject.SetActive(true);
        textObjects[screenLocation].transform.parent.gameObject.SetActive(true);
    }

    private void CreateBarChartObject(List<float> values, int screenLocation)
    {
        foreach (GameObject chartObject in gameObjectList)
        {
            Destroy(chartObject);
        }
        gameObjectList.Clear();

        float graphHeight = lineBarChartTransforms[screenLocation].sizeDelta.y;
        float graphWidth = lineBarChartTransforms[screenLocation].sizeDelta.x;

        int maxVisibleValueAmount = 10;

        float yMax = values[0];
        float yMin = values[0];

        for (int i = Mathf.Max(values.Count - maxVisibleValueAmount, 0); i < values.Count; i++)
        {
            float value = values[i];

            if (value > yMax)
                yMax = value;

            if (value < yMin)
                yMin = value;
        }

        float yDifference = yMax - yMin;

        if (yDifference < .5f && yDifference > 0f)
        {
            yDifference = 5f;
        }

        yMax += yDifference * .2f;
        yMin -= yDifference * .2f;

        float xSize = graphWidth / (maxVisibleValueAmount + 1);

        int xIndex = 0;

        for (int i = Mathf.Max(values.Count - maxVisibleValueAmount, 0); i < values.Count; i++)
        {
            float xPos = xSize + xIndex * xSize;
            float yPos = (values[i] - yMin) / (yMax - yMin) * graphHeight;

            GameObject barObject = CreateBar(new Vector2(xPos, yPos), xSize * .7f, screenLocation);
            gameObjectList.Add(barObject);

            RectTransform labelX = Instantiate(labelTemplateX[screenLocation]);
            labelX.SetParent(lineBarChartTransforms[screenLocation]);
            labelX.gameObject.SetActive(true);
            labelX.anchoredPosition = new Vector2(xPos, -3f);
            labelX.GetComponent<Text>().text = (i+1).ToString();
            gameObjectList.Add(labelX.gameObject);

            xIndex++;
        }

        int seperatorCount = values.Count;
        for (int i = 0; i <= seperatorCount; i++)
        {
            RectTransform labelY = Instantiate(labelTemplateY[screenLocation]);
            labelY.SetParent(lineBarChartTransforms[screenLocation]);
            labelY.gameObject.SetActive(true);
            float normalizedValue = i * 1f / seperatorCount;
            labelY.anchoredPosition = new Vector2(-3f, normalizedValue * graphHeight);
            labelY.GetComponent<Text>().text = (yMin + normalizedValue * (yMax - yMin))
                .ToString("0.0");
            gameObjectList.Add(labelY.gameObject);
        }

        lineBarChartObjects[screenLocation].SetActive(true);
        lineBarChartObjects[screenLocation].transform.parent.gameObject.SetActive(true);
        textObjects[screenLocation].transform.parent.gameObject.SetActive(true);
    }

    private GameObject CreateDotConnection(Vector2 posA, Vector2 posB, int screenLocation)
    {
        GameObject connectionObject = new GameObject("dotConnection", typeof(Image));
        connectionObject.transform.SetParent(lineBarChartTransforms[screenLocation], false);
        connectionObject.GetComponent<Image>().color = new Color(1,1,1, .5f);
        RectTransform rectTransform = connectionObject.GetComponent<RectTransform>();
        Vector2 dir = (posB - posA).normalized;
        float distance = Vector2.Distance(posA, posB);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 4f);
        rectTransform.anchoredPosition = posA + dir * distance * .5f;
        rectTransform.localEulerAngles = new Vector3(0, 0, 
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        return connectionObject;
    }

    private GameObject CreateBar(Vector2 barPos, float barWidth, int screenLocation)
    {
        GameObject barObject = new GameObject("bar", typeof(Image));
        barObject.transform.SetParent(lineBarChartTransforms[screenLocation], false);
        RectTransform rectTransform = barObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(barPos.x, 0f);
        rectTransform.sizeDelta = new Vector2(barWidth, barPos.y);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(.5f, 0f);
        return barObject;
    }

    private void MakeAllObjectsHidden()
    {
        for (int i = 0; i < 4; i++)
        {
            textObjects[i].SetActive(false);
            imageObjects[i].SetActive(false);
            pieChartObjects[i].SetActive(false);
            lineBarChartObjects[i].SetActive(false);

            lineBarChartObjects[i].transform.parent.gameObject.SetActive(false);
            textObjects[i].transform.parent.gameObject.SetActive(false);
        }

        inputButtonObject.SetActive(false);
    }

    public void ModeHasChanged()
    {
        repeaterActive = false;

        if (modeDropdown.value == 2)
        {
            PassiveModeSelected();
            RepeatCommunication();
            return;

        }

        inputTitleObject.SetActive(true);
        inputDropdownObject.SetActive(true);
        inputFieldObject.SetActive(true);
        refreshButtonObject.SetActive(true);
    }

    public void PassiveModeSelected()
    {
        refreshButtonObject.SetActive(false);
        inputTitleObject.SetActive(false);
        inputDropdownObject.SetActive(false);
        inputFieldObject.SetActive(false);

        if (controlPanelAnimator != null && controlButtonAnimator != null)
        {
            controlPanelAnimator.SetBool("hiding", true);
            controlButtonAnimator.SetBool("hiding", true);
        }
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
            name = modifiableNames[inputDropdown.value],
            operation = "modify",
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
        inputField.text = null;
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
            yield return new WaitForSeconds(3);
        }
    }

    private IEnumerator CommunicateWithServer()
    {
        string url = BASE_URL + "debug/temperature/";

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
        modifiableNames.Clear();

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

            modifiableNames.Add(chartResponse.data[i].name);

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
                    previousImageLink = stringValues[0];
                }
                break;
            case "pie":
                if (floatValues != null)
                {
                    CreatePieChartObject(floatValues, data.screenLocation - 1);
                }
                break;
            case "line":
                if (floatValues != null)
                {
                    horizontalTitleObjects[data.screenLocation - 1].text = 
                        data.labels.horizontal;
                    verticalTitleObjects[data.screenLocation - 1].text =
                        data.labels.vertical;
                    CreateLineChartObject(floatValues, data.screenLocation - 1);
                }
                break;
            case "bar":
                if (floatValues != null)
                {
                    CreateBarChartObject(floatValues, data.screenLocation - 1);
                }
                break;
            case "data-text":
                if (data.text != null)
                {
                    CreateTextObject(data.text, data.screenLocation - 1);
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
        if (imageLink == previousImageLink)
        {
            imageObjects[screenLocation].SetActive(true);
            imageObjects[screenLocation].transform.parent.gameObject.SetActive(true);
            return;
        }

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
