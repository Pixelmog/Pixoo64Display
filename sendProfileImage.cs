using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO; 
using System.Drawing; 

public class CPHInline
{
	public string deviceID = Environment.GetEnvironmentVariable("pixooDeviceId"); //you can find this information with the getDevice() function
	public string devicePrivateIP = Environment.GetEnvironmentVariable("pixooDevicePrivateIP"); 
	public string deviceMAC = Environment.GetEnvironmentVariable("pixooDeviceMAC"); 
	
	public int picId; //Every time an image is updated, it needs a unique ID. 
	//This is connected to a persisted global variable in Streamer.Bot 
	
	public string profileImageUrl; //the profileImageURL for the person that redeems the channel point reward. 
	public string userInput; //the message the user types in to the channel point reward. 

	public bool Execute()
	{
		CPH.TryGetArg("targetUserProfileImageUrl", out profileImageUrl);  //usersProfileImage
		CPH.TryGetArg("rawInput", out userInput); //users Input into channel point redemption
		picId = CPH.GetGlobalVar<int>("picId", true); //get the current picID
		CPH.SetGlobalVar("picId", picId + 1, true); //increment the picID

		sendProfilePicture(); //Sends the profile picture of the user to the Pixoo64
		CPH.Wait(2000);
		sendMessage(); //Prints text to run over the image 
		
		//getDevice(); //Use this function first to get your device details. 
		//getDialList(); //get a list of "clocks" that have been set up on the app. 
		//changeFace(); //Can change to other "clocks", set up from the app. 
		return true;
	}
	
    public async Task getDevice() //this function is used to get the necessary device IDs
    {
        using HttpClient client = new HttpClient();
        string url = "https://app.divoom-gz.com/Device/ReturnSameLANDevice";
        StringContent content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        string result = await response.Content.ReadAsStringAsync();
        CPH.LogInfo($"Status: {response.StatusCode} - {response.ReasonPhrase}");
        CPH.LogInfo($"Response: {result}");
    }
    
    public async Task getDialList() //this function is to get the list of Clock faces set up, with their IDs. 
    {
        using var client = new HttpClient();
        var url = "https://app.divoom-gz.com/Channel/GetDialList";
        
        
        string request = $@"
        {{
        	""DialType"": ""Social"", 
        	""Page"": 1
        }}";
        
        StringContent content = new StringContent(request, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        string result = await response.Content.ReadAsStringAsync();
        CPH.LogInfo($"Status: {response.StatusCode} - {response.ReasonPhrase}");
        CPH.LogInfo($"Response: {result}");
    }
    
    public async Task changeFace() //used to change "clocks"
    {
		using var client = new HttpClient(); 
		string url = $"http://{devicePrivateIP}/post";
		string request = $@"
        {{
        	""Command"": ""Channel/SetClockSelectId"", 
        	""ClockId"": 248
        }}";
        
        var content = new StringContent(request, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        string result = await response.Content.ReadAsStringAsync();
        CPH.LogInfo($"Status: {response.StatusCode} - {response.ReasonPhrase}");
        CPH.LogInfo($"Response: {result}");
  
    }
    
	public static byte[] ConvertTo64x64RGB(byte[] imageBytes) 
    {
    	//this is a helper function to change the profile image into an array of the RGB data for the pixels. 
        using var ms = new MemoryStream(imageBytes);
        using var original = new Bitmap(ms);
        using var resized = new Bitmap(original, new Size(64, 64));

        byte[] rgbBytes = new byte[64 * 64 * 3];
        int i = 0;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color pixel = resized.GetPixel(x, y);
                rgbBytes[i++] = pixel.R;
                rgbBytes[i++] = pixel.G;
                rgbBytes[i++] = pixel.B;
            }
        }
        return rgbBytes;
    }
     
    public async Task sendProfilePicture()
    {
		using HttpClient client = new HttpClient(); 
		int width = 64; 
		int height = 64; 
		string url = $"http://{devicePrivateIP}/post";
		byte[] imageBytes; 
		
		imageBytes = await client.GetByteArrayAsync(profileImageUrl); 
		byte[] rgbData = ConvertTo64x64RGB(imageBytes); 
		string base64Data = Convert.ToBase64String(rgbData);
        string payload = $@"
        {{
            ""Command"": ""Draw/SendHttpGif"",
            ""PicNum"": 1,
            ""PicWidth"": {width},
            ""PicOffset"": 0,
            ""PicID"": {picId},
            ""PicData"": ""{base64Data}""
        }}";
        
        StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        string result = await response.Content.ReadAsStringAsync();
        CPH.LogInfo($"Status: {response.StatusCode} - {response.ReasonPhrase}");
        CPH.LogInfo($"Response: {result}");
  
    }
  
    public async Task sendMessage()
    {
		using HttpClient client = new HttpClient(); 
		string url = $"http://{devicePrivateIP}/post";
	
		string clearRequest = $@"
		{{
			""Command"":""Draw/ClearHttpText""
		}}"; 
		
		string request = $@"
        {{
        	""Command"": ""Draw/SendHttpText"", 
        	""TextId"": 2, 
			""x"": 0, 
			""y"": 40, 
			""dir"": 0, 
			""font"": 8, 
			""TextWidth"": 56, 
			""speed"": 10, 
			""TextString"": ""{userInput}"",
			""color"": ""000000"",
			""align"": 1
        }}";
        
        StringContent clearContent = new StringContent(clearRequest, Encoding.UTF8, "application/json");
        var clearResponse = await client.PostAsync(url, clearContent);
        string clearResult = await clearResponse.Content.ReadAsStringAsync(); 
        CPH.LogInfo($"Status: {clearResponse.StatusCode} - {clearResponse.ReasonPhrase}");
        CPH.LogInfo($"Response: {clearResult}");
        
        StringContent content = new StringContent(request, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        string result = await response.Content.ReadAsStringAsync();
        CPH.LogInfo($"Status: {response.StatusCode} - {response.ReasonPhrase}");
        CPH.LogInfo($"Response: {result}");
    }
    
}

