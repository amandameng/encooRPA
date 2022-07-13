//代码执行入口，请勿修改或删除
public void Run()
{
   try{
	//Console.WriteLine(picad);
	
	//JObject jo = (JObject)JsonConvert.DeserializeObject(picad);
	//string errorCode = jo["errCode"].ToString();
	
       
	//if(errorCode == "0"){
	string zone_en = picad; // 148, 80
	string[] strArray = zone_en.Split(',');
	int xmove = Convert.ToInt32(strArray[0]); // 148
	
	int sliderMargin = 20;
	int sliderX = sliderIuiObj.BoundingRectangle.X - backIuiObj.BoundingRectangle.X + sliderMargin;
	Console.WriteLine("sliderX, {0}", sliderX);
	xmoveTo = xmove - sliderX;
    Console.WriteLine("xmoveTo, {0}", xmoveTo);
	//}
	//else{
	//  errorMsg = jo["errMsg"].ToString();
	//}
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
    
}
//在这里编写您的函数或者类