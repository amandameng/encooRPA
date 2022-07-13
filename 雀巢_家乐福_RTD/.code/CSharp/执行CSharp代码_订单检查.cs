public decimal 家乐福扣点 = 22.08m;

//代码执行入口，请勿修改或删除
public void Run()
{   
    outExceptionDT = exceptionOrder模板数据表.Clone();
    DataTable sourceTable = 源订单数据表.Copy();
    
    跳过后续检查 = 异常原因!=null && 异常原因.Contains("已取消");
    
    IEnumerable<IGrouping<string, DataRow>> groupedOrders = 源订单数据表.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["order_number"].ToString());//C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值
    // string orderFirstBU = String.Empty;
    foreach (var itemGroup in groupedOrders)
    {
        DataRow[] orderItemRows = itemGroup.ToArray();
        string orderFirstBU = String.Empty;
        foreach(DataRow dr in orderItemRows){
            string curBU = dr["BU"].ToString().Trim();
            if(!string.IsNullOrEmpty(curBU)){
                orderFirstBU = curBU;
                break;
            }
        }
       // DataRow[] buNotNullRows = orderItemRows.Select("BU is not null");

         // 产品主数据无法mapping时，该行产品PO不留空，将PO默认等于已有mapping出的PO，如NPP产品PO，订单只会都下NPP的产品，当mapping到其他一个产品有mapping关系后，确认该单PO No为***NPP后，该单所有产品PO全部填充为***NPP
       // string orderFirstBU = (buNotNullRows.Count() > 0) ? buNotNullRows[0]["BU"].ToString() : String.Empty;

      // 整合一个order里面的items转换成exception order 模板的数据
        List<DataRow> initExceptionRows = new List<DataRow>{};
        bool hasPriceIssue = false; // 如果一个订单任意1个或者多个产品遇到价差异常，那么其余产品也需要在exception order列出来，但是exception reason 留空
       
       // Console.WriteLine("orderItemRows Count:{0}", orderItemRows.Length);
       
        foreach(DataRow orderItemDRow in orderItemRows){
            DataRow exceptionDRow = outExceptionDT.NewRow();
          //  Console.WriteLine("异常原因: {0}", 异常原因);
            exceptionDRow["问题分类"] = 异常原因;
            exceptionDRow["问题详细描述"] = 异常详细描述;
            handleExceptionRow(orderItemDRow, orderFirstBU, ref exceptionDRow);
            
            // Console.WriteLine("问题分类: {0}", exceptionDRow["问题分类"].ToString());
            
            if(exceptionDRow["问题分类"].ToString().Contains("价格差异")){ // 如果有价差差异
                if(hasPriceIssue == false){
                    hasPriceIssue = true; // 已经设置过价差判断
                }
            }
            initExceptionRows.Add(exceptionDRow);
        }
        
      //  Console.WriteLine("initExceptionRows: {0}", initExceptionRows.Count);
        // 依次判断当前订单是否有价差问题，如果有，则所有items都要展示出来
        foreach(DataRow dr in initExceptionRows){
          string 异常描述 = dr["问题分类"].ToString();
            // 异常描述为空并且包含价差问题
          if(string.IsNullOrEmpty(异常描述) && hasPriceIssue){
              dr["问题分类"] = " ";
          }
          outExceptionDT.Rows.Add(dr);
        } 
    }
   // Console.WriteLine(Convert.ToInt32("a,b"));
}

//在这里编写您的函数或者类
public void handleExceptionRow(DataRow orderItemDRow, string orderFirstBU, ref DataRow exceptionDRow){
    List<Dictionary<string, string>> 问题分类描述 = new List<Dictionary<string, string>>{};
    // bool 跳过后续检查 = false;
    exceptionDRow["渠道"] = "01 NKA";
    exceptionDRow["客户名称"] = GlobalVariable.VariableHelper.GetVariableValue("客户平台");
    DateTime orderCreateDateTime = DateTime.Parse(orderItemDRow["create_date"].ToString());
    exceptionDRow["订单日期"] = orderCreateDateTime.ToString("yyyy/MM/dd");
    exceptionDRow["客户PO"] = orderItemDRow["order_number"];
    exceptionDRow["客户产品代码"] = orderItemDRow["product_code"];
    exceptionDRow["雀巢产品代码"] = orderItemDRow["雀巢产品编码"];
    exceptionDRow["Plant/区域"] = orderItemDRow["nestle_plant_no"];
    exceptionDRow["产品名称"] = orderItemDRow["product_name"];
    exceptionDRow["BU"] = orderItemDRow["BU"];
    exceptionDRow["雀巢 SAP PO"] = getSAPPO(orderItemDRow, orderFirstBU);
    exceptionDRow["家乐福大仓描述"] = orderItemDRow["logistics_warehouse"];
    exceptionDRow["箱价"] = orderItemDRow["雀巢产品箱价"];
    DateTime requestDeliveryDate = DateTime.Parse(orderItemDRow["request_delivery_date"].ToString());
    exceptionDRow["客户要求送货日"] = requestDeliveryDate.ToString("yyyy/MM/dd");
    exceptionDRow["客户价格"] = orderItemDRow["unit_price"];
    string 订单箱规 = orderItemDRow["package_size"].ToString();
    
    string 订单箱规数字字符 = 获取订单箱规(订单箱规);
    string 雀巢产品箱规 = orderItemDRow["雀巢产品箱规"].ToString();
    
    decimal 产品确认数量 = Convert.ToDecimal(orderItemDRow["confirm_qty"].ToString());
    decimal 订单箱规数字 = Convert.ToDecimal(订单箱规数字字符);
    decimal 箱数= 产品确认数量 / 订单箱规数字;
    int 箱数整数 = Convert.ToInt32(箱数);
    exceptionDRow["数量"] = 箱数.Equals(箱数整数) ? 箱数整数 : 箱数;
    int 在途天数 = toIntConvert(orderItemDRow["在途天数"]);
    在途天数 = 在途天数 == 0 ? 1 : 在途天数;
    DateTime todayDate = DateTime.Today;

    // Console.WriteLine("----订单箱规----{0}", 订单箱规);
    // Console.WriteLine("=====问题分类====={0}=========", exceptionDRow["问题分类"].ToString());
    /* 
      1、  已取消订单 
             或者
           订单修改RDD的流程 
           两种整单异常情况，跳过后续的检查。
      2、  指定送货日虽然也是整单检查，但是也是要检查价差的，所以后面的异常检查仍然需要做
    */
   
    //  已取消订单 跳过后续检查
    if(跳过后续检查){
        setExceptionMessage(ref exceptionDRow, 问题分类描述);
        return;
    }
    // 订单RDD比（读单日期）大于等于在途时间则是clean
    
    if(requestDeliveryDate <= todayDate.AddDays(在途天数)){
        Dictionary<string, string> 问题字典1 = new  Dictionary<string, string>{};
        问题字典1["问题分类"] = "送货日异常";
        问题字典1["问题描述"] = "送货日异常";
        问题分类描述.Add(问题字典1);
        //setExceptionMessage(ref exceptionDRow, 问题分类描述);
        //return;
    }
    
   // 同一张订单，RDD是否符合送货日要求，如果不符合，问题反馈为，修改RDD,送货日异常；如果符合要求，问题反馈为修改RDD 
    //if(异常原因!=null && 异常原因 == "订单修改RDD"){
    string ststRDD =  orderItemDRow["ststRDD"].ToString();
    string[] rddArr = ststRDD.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
    
    List<string> dayStringList = new List<string> {};
    foreach(string day in rddArr)
    {
        string newDay = day.Replace(" ", "");
        dayStringList.Add(newDay);
    }
    
    string 周几 = CaculateWeekDay(requestDeliveryDate);
    
    // 交货日期不在定义的范围内时，标记为异常订单
    if(!dayStringList.Contains(周几)){
        Dictionary<string, string> 问题字典2 = new Dictionary<string, string>{};
        问题字典2["问题分类"] = "指定送货日";
        问题字典2["问题描述"] = "客户网站交货日期不符合车期";
        问题分类描述.Add(问题字典2);
    }

    bool mabdInNextMonth = (requestDeliveryDate.ToString("yyyy-MM") == todayDate.AddMonths(1).ToString("yyyy-MM"));
    if(mabdInNextMonth){
        Dictionary<string, string> 问题字典_指定送货日= new  Dictionary<string, string>{};
        问题字典_指定送货日["问题分类"] = "跨月订单";
        问题字典_指定送货日["问题描述"] = "跨月订单";
        问题分类描述.Add(问题字典_指定送货日);
    }
    
    // 特殊产品异常判断
    string remark = orderItemDRow["Remark"].ToString();
    //Console.WriteLine("客户PO---{0}, remark---{1}, 客户产品代码---{2}", orderItemDRow["order_number"].ToString(), remark, exceptionDRow["客户产品代码"]);
    if(remark.Contains("特殊产品")){
        Dictionary<string, string> 问题字典3= new  Dictionary<string, string>{};

        问题字典3["问题分类"] = "特殊产品";
        问题字典3["问题描述"] = "特殊产品";
        问题分类描述.Add(问题字典3);
        // setC4Info(orderItemDRow, ref exceptionDRow,  箱数, 订单箱规数字, orderFirstBU);
        setExceptionMessage(ref exceptionDRow, 问题分类描述);
        return;
    }

    string 雀巢产品编码 = orderItemDRow["雀巢产品编码"].ToString();
    if(String.IsNullOrEmpty(雀巢产品编码)){ // 未匹配到雀巢主产品对应编码, ！！！价差检查没必要做
        // errorCode = "30000"; // 03. 产品编码有误
        Dictionary<string, string> 问题字典4 = new  Dictionary<string, string>{};

        问题字典4["问题分类"] = "产品编码有误";
        问题字典4["问题描述"] = "客户网站产品编码无法通过信息表mapping出雀巢产品";
        问题分类描述.Add(问题字典4);
        // setC4Info(orderItemDRow, ref exceptionDRow,  箱数, 订单箱规数字, orderFirstBU);
        setExceptionMessage(ref exceptionDRow, 问题分类描述);
        return;
    }
    
    if(跳过异常检查){
      // 异常信息赋值
      setExceptionMessage(ref exceptionDRow, 问题分类描述);
      return;
    }

    if(订单箱规数字字符 != 雀巢产品箱规){
        // 05. 产品箱规不一致
        // return;
        Dictionary<string, string> 问题字典5 = new  Dictionary<string, string>{};

        问题字典5["问题分类"] = "产品箱规不一致";
        问题字典5["问题描述"] = String.Format("客户系统产品包装规格和雀巢包装规格不一致，客户箱规：{0}， 雀巢箱规：{1}", 订单箱规数字字符, 雀巢产品箱规);
        
        问题分类描述.Add(问题字典5);
    }
    

    if (!箱数.Equals(Convert.ToInt32(箱数)))
    {
        // 06. 订单数量取整检查
        Dictionary<string, string> 问题字典6 = new  Dictionary<string, string>{};

        问题字典6["问题分类"] = "订单数量取整检查";
        问题字典6["问题描述"] = "订单数据量不是箱规的整数倍";
        
        问题分类描述.Add(问题字典6);
    }
        
    bool 价差有问题 = 检查价差(orderItemDRow, ref exceptionDRow, 箱数, 订单箱规数字);
    if(价差有问题){
        // 02. 价格差异
        // Console.WriteLine("*******: {0}", 问题字典["问题分类"]);
        Dictionary<string, string> 问题字典7 = new  Dictionary<string, string>{};

        问题字典7["问题分类"] = "价格差异";
        问题字典7["问题描述"] = "不符合价差检查标准";
         
        问题分类描述.Add(问题字典7);
    }
    
    // 异常信息赋值
    setExceptionMessage(ref exceptionDRow, 问题分类描述);
}

public string 获取订单箱规(string 订单箱规){
    Regex 数字正则 = new Regex(@"(\d+)");
    Match matchResult = 数字正则.Match(订单箱规);
    string 箱规数字 = matchResult.Value;
    return 箱规数字;
}

public void setC4Info(DataRow orderItemDRow, ref DataRow exceptionDRow, decimal 箱数, decimal 订单箱规数字, string orderFirstBU){
    decimal 含税单价 = Convert.ToDecimal(orderItemDRow["unit_price"].ToString());
    decimal npsRate = (decimal)(家乐福扣点/100); // 固定值
    
    string tax_point = orderItemDRow["Tax_Point"].ToString();
    if(string.IsNullOrEmpty(tax_point)){
        string nppStr = String.IsNullOrEmpty(orderItemDRow["BU"].ToString()) ? orderFirstBU : orderItemDRow["BU"].ToString(); // 客户-雀巢主产品表里面的字段
        tax_point = (nppStr.ToUpper() == "NPP") ? "0.09" : "0.13";
    }
    decimal 税率 = 1 + Convert.ToDecimal(tax_point);
    decimal 刨去扣点 = 1 - npsRate;
   
    decimal 雀巢GPS价 = (含税单价 / 税率) * 订单箱规数字; // 客户平台不含税箱价
    decimal C4开票价 = 雀巢GPS价 * 箱数 * 刨去扣点; // 等于订单税前总价刨掉扣点, 客户平台不含税价箱价 * 箱数 * 刨去扣点
    exceptionDRow["C4开票价"] = Math.Round(C4开票价, 6);
}

public bool 检查价差(DataRow orderItemDRow, ref DataRow exceptionDRow, decimal 箱数, decimal 订单箱规数字){
    string bu = orderItemDRow["BU"].ToString();
    string NPP跳过价差价差 = etoConfigDT.Rows[0]["skip_npp_price_check"].ToString();
    string 扣点 = etoConfigDT.Rows[0]["skip_npp_price_check"].ToString();

    if(bu == "NPP" && NPP跳过价差价差 == "1"){ // NPP 订单是否跳过价差检查
        return false;
    }
    Console.WriteLine("雀巢产品箱价: {0}， order_number：{1}, customer_product_code: {2}", orderItemDRow["雀巢产品箱价"].ToString(), orderItemDRow["order_number"].ToString(), orderItemDRow["product_code"].ToString());
    decimal NPS价 = 0;
    // decimal 雀巢产品调价 = 0;
    NPS价 = toDecimalConvert(orderItemDRow["雀巢产品箱价"]);
   /*
   try{
        
        // 雀巢产品调价 = Convert.ToDecimal(orderItemDRow["雀巢产品调价"].ToString());
    }catch(Exception e){
        Console.WriteLine(e.Message);
        // return true; // 价格不合法
    }
    */

    decimal 含税单价 = toDecimalConvert(orderItemDRow["unit_price"]);
    
    decimal npsRate = (decimal)(家乐福扣点/100); // 固定值
    string tax_point = orderItemDRow["Tax_Point"].ToString();
    if(string.IsNullOrEmpty(tax_point)){
        tax_point = "0.13";
    }

    decimal 税率 = 1 + toDecimalConvert(tax_point);
    decimal 刨去扣点 = 1 - npsRate;
   
    // decimal 家乐福价 = (含税单价 / 税率) * 订单箱规数字;
    decimal 雀巢GPS价 = (含税单价 / 税率) * 订单箱规数字; // 客户平台不含税箱价
    
    // if User_Remark 不为空，并且包含tpp扣点
    string userRemark = orderItemDRow["User_Remark"].ToString().Trim();
	decimal TPP扣点= 0;
    decimal NET价 = 0;
    if(!string.IsNullOrEmpty(userRemark) && userRemark.Contains("tpp")){
        TPP扣点 = fetchRateInDecimal(userRemark);
        NET价 = NPS价 * 箱数 * (刨去扣点 - TPP扣点);
    }else{
        NET价 = NPS价 * 箱数 * 刨去扣点;          // 雀巢箱价 * 箱数 * 刨去扣点
        // TPP扣点 = 1 - NET价 / (NPS价*箱数) - npsRate; // NET价, 价格检查excel W列
    }
    
    decimal 下单扣点 = NPS价 == 0 ? 1 : (1 - 雀巢GPS价 / NPS价); // 订单税前总单价/雀巢箱价
    decimal 降价扣点 = 1 - NPS价 / NPS价; // 调价, 价格检查excelAJ列， 数据库 -> 雀巢主产品表 -> 调价

    decimal 实际扣点 = 下单扣点 - 降价扣点 - TPP扣点;
    decimal C4开票价 = 雀巢GPS价 * 箱数 * 刨去扣点; // 等于订单税前总价刨掉扣点, 客户平台不含税价箱价 * 箱数 * 刨去扣点
    decimal 价差 = C4开票价 - NET价;
    Console.WriteLine("雀巢GPS价(客户平台不含税箱价): {0}, 雀巢产品箱价(NPS价): {1}, 含税单价:{2}, NET价:{3}, 下单扣点:{4}，降价扣点：{5}， TPP扣点：{6}， 实际扣点！！： {7}， 价差！！： {8}, C4开票价: {9}", 雀巢GPS价, NPS价, 含税单价, NET价, 下单扣点, 降价扣点, TPP扣点, 实际扣点, 价差, C4开票价);
    exceptionDRow["C4开票价"] = Math.Round(C4开票价, 6);
    exceptionDRow["价差"] = Math.Round(价差, 6);
    exceptionDRow["TPP扣点"] = Math.Round(TPP扣点, 6);
    exceptionDRow["实际扣点"] = Math.Round(实际扣点, 6);
    exceptionDRow["雀巢价格"] = Math.Round((NPS价/订单箱规数字), 6);

    // 实际扣点的绝对值大于1% 并且 价差绝对值大于1
    if((Math.Abs(实际扣点) > (decimal)0.01) && Math.Abs(价差) > (decimal)1){
        return true;
    }
    return false;
}

// 根据规则输出 雀巢方的PO Number
public string getSAPPO(DataRow dr, string orderFirstBU){
    string poNumber = dr["order_number"].ToString();
    string 仓库地址 = dr["logistics_warehouse"].ToString(); // 对应订单里面的仓库地址
    string 仓库短码 = dr["order_type_short"].ToString(); // 对应 ship to sold to 表里面的字段
    string nppStr = String.IsNullOrEmpty(dr["BU"].ToString()) ? orderFirstBU : dr["BU"].ToString(); // 客户-雀巢主产品表里面的字段
    string order_type = dr["order_type"].ToString();// order_type
    if(String.IsNullOrEmpty(仓库短码)){
       仓库短码 = "CH"; // 默认CH
    }
    if(order_type.Contains("越库")){
       仓库短码 = "YK"; 
    }
    return String.Format("{0}{1}{2}RTD", poNumber, 仓库短码, nppStr.ToUpper() == "NPP" ? "NPP" : "");
}


public void setExceptionMessage(ref DataRow exceptionDRow, List<Dictionary<string, string>> 问题分类描述){
    if(问题分类描述.Count > 0){
        Dictionary<string, string> resultDic = combineExceptionMessage(问题分类描述);
        // 问题分类为空的话，直接赋值，否则增量赋值
        string 问题分类 =  !String.IsNullOrEmpty(exceptionDRow["问题分类"].ToString()) ?  (exceptionDRow["问题分类"].ToString() + "\n" + resultDic["问题分类"]) : resultDic["问题分类"];
        string 问题详细描述 = !String.IsNullOrEmpty(exceptionDRow["问题详细描述"].ToString()) ?  (exceptionDRow["问题详细描述"].ToString() + "\n" + resultDic["问题详细描述"]) : resultDic["问题详细描述"];
        exceptionDRow["问题分类"] = 问题分类;
        exceptionDRow["问题详细描述"] = 问题详细描述;
    }
}

// 根据list of 字典异常数据，合并（\n）异常信息
public Dictionary<string, string> combineExceptionMessage(List<Dictionary<string, string>> 问题分类描述 ){
    List<string> 问题分类 = new List<string>{};
    List<string> 问题详细描述 = new List<string>{};
 
    foreach(var item in 问题分类描述){
        /*Console.WriteLine(string.Join("!!", item.Keys));
        foreach(var kv in item){
            Console.WriteLine("问题分类描述666 - key：{0}， Value: {1}", kv.Key, kv.Value);
          
        }
        */
        问题分类.Add(item["问题分类"]);
        问题详细描述.Add(item["问题描述"]);
    }
    string 问题分类Str = String.Join("\n", 问题分类);
    string 问题详细描述Str = String.Join("\n", 问题详细描述);
    Dictionary<string, string> resultDic = new Dictionary<string, string> {};
    resultDic["问题分类"] = 问题分类Str;
    resultDic["问题详细描述"] = 问题详细描述Str;
    return resultDic;
}

public string CaculateWeekDay(DateTime dtNow)
{
    var weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    return weekdays[(int)dtNow.DayOfWeek];
}

public static decimal fetchRateInDecimal(string walmartDiscountRateStr)
{
    Regex 百分数正则 = new Regex(@"\d+\.?\d{0,2}%$");
    Match matchResult = 百分数正则.Match(walmartDiscountRateStr);
    string 百分比 = matchResult.Value;
    decimal resutRate = 0;
    try
    {
        if (!string.IsNullOrEmpty(百分比))
        {
            resutRate = Convert.ToDecimal(百分比.Replace("%", "")) / 100m;
        }
        else
        {
            if (!walmartDiscountRateStr.Contains("%"))
            { // 不包含%
                resutRate = Convert.ToDecimal(walmartDiscountRateStr);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("walmartDiscountRateStr不合法： {0}", e.Message);
    }
    return resutRate;
}

public static decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
        Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

public static int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
        Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}