//代码执行入口，请勿修改或删除
const string 订货日字符 = "订货日：";
const string 预定交货日字符 = "预定交货日:";
const string 地址字符 = "地址：";
const string fromStr = "FROM：";
const string 预定退货日字符 = "预订退货日：";
const string 退货截止日字符 = "退货截止日:";


public void Run()
{
  
        string rawPdfstr = pdfStr;
        pdfStr = Regex.Replace(pdfStr, @"\s+", "  ");
        var pdfArray = pdfStr.Split(' ');
        
        // Console.WriteLine(string.Join("|",pdfArray));
  
        #region 退货单处理
        if(pdfStr.IndexOf("预订退货日") > -1){
            int index = 0;
            bool nextTableData = false;
            int tableDataIndex = 0;
            
            string 订单类型 = "退货单";
            string 订单编号 = "";
            Dictionary<string, string> 主要信息 = new Dictionary<string, string> { };

            fetchItemByLineSeperator(rawPdfstr, 预定退货日字符, ref 主要信息);
            string 预定退货日 = Regex.Split(主要信息[预定退货日字符], @"\s+", RegexOptions.IgnorePatternWhitespace)[0];

            fetchItemByLineSeperator(rawPdfstr, 退货截止日字符, ref 主要信息);
            string 退货截止日期 = Regex.Split(主要信息[退货截止日字符], @"\s+", RegexOptions.IgnorePatternWhitespace)[0];

            fetchItemByLineSeperator(rawPdfstr, 地址字符, ref 主要信息);
            string 地址 = 主要信息[地址字符];
            fetchItemByLineSeperator(rawPdfstr, fromStr, ref 主要信息);
            string from = 主要信息[fromStr];
            string[] fromArr = Regex.Split(from, @"\s+", RegexOptions.IgnorePatternWhitespace);
            string WMDCCode = fromArr[0].Trim();
            string 送货店 = fromArr[1];
 
            /*bool 第二個元素是第 = pdfArray[2] == "第";
            string 预定退货日 = 第二個元素是第 ? pdfArray[24] : pdfArray[4] ;
            string 退货截止日期 = 第二個元素是第 ? pdfArray[28] : pdfArray[8] ;
            
            string 地址 = 第二個元素是第 ? (pdfArray[30] == "0" ? pdfArray[54] : pdfArray[52]) : pdfArray[52];
            string 送货店 = 第二個元素是第 ? (pdfArray[30] == "0" ? pdfArray[46] : pdfArray[44]) : pdfArray[44];
            */
            DataRow orderRow = ordersTable.NewRow();   
            string 备注 = string.Format("{0} {1} {2}",pdfArray[pdfArray.Length - 5], pdfArray[pdfArray.Length - 3], pdfArray[pdfArray.Length - 1]);
            
            string 商品货号 = "";
            string 商品名称 = "";
            string 品牌 = "";
            string 规格 = "";
            string 单位 = "";
            string 订购倍数 = "";
            string 总订个数 = "";
            string 商品条码 = "";
            string 产地 = "";
            string QS = "";
            
            bool skipFlag = false;
            bool isFinishRow = false;
            int PO序号 = 1;
            
            foreach(string pdfItem in pdfArray){
                if(pdfItem.IndexOf("订单编号") > -1){
                    订单编号 = pdfArray[index + 2];
                    orderRow["OrderType"] = 订单类型;
                    orderRow["OrderNumber"] = 订单编号;
                    orderRow["OrderDate"] = Convert.ToDateTime(预定退货日);
                    orderRow["ShipDate"] = Convert.ToDateTime(退货截止日期);
                    orderRow["ShipAddress"] = 地址;
                    orderRow["WMDC"] = WMDCCode;
                    orderRow["ShipFrom"] = 送货店;
                    orderRow["TotalUnit"] = 0;
                    ordersTable.Rows.Add(orderRow);
                }
                if(pdfItem == "金额"){
                    nextTableData = true;
                    continue;
                }
                if(pdfItem.IndexOf("备注") > -1){
                    if(!skipFlag) skipFlag = true;
                    else break;
                }
                else if(nextTableData){
                    switch (tableDataIndex) {
                        case 1 : //商品货号
                            商品货号 = pdfItem;
                            break;
                        case 3 : //商品名称
                            商品名称 = pdfItem;
                            break;
                        case 5 : //品牌
                            品牌 = pdfItem;
                            break;
                        case 7 : //规格
                            规格 = pdfItem;
                            break;
                        case 9 : //单位
                            单位 = pdfItem;
                            break;
                        case 11 : //总订个数
                            总订个数 = pdfItem.Trim().Replace(",","").Replace("-","");
                            break;
                        case 13 : {//商品条码
                            if(pdfItem == "见商品包装"){
                                商品条码 = "";
                            }
                            else 商品条码 = pdfItem;
                            break;
                        }
                        case 15 : {//产地
                            if(pdfItem != "见商品包装"){
                                产地 = "见商品包装";
                            }
                            else 产地 = pdfItem;
                            isFinishRow = true;
                            break;
                        }
                        default:
                            break;
                    };
                    if(isFinishRow) {
                        //写入Exception
                        DataRow exceptionRow = ExceptionTable.NewRow();
                        exceptionRow["渠道"] = "IB";
                        exceptionRow["订单日期"] = 预定退货日;
                        exceptionRow["客户名称"] = "全家";
                        exceptionRow["客户PO"] = 订单编号;
                        exceptionRow["雀巢SAP_PO"] = "IBU" + 订单编号;
                
                        if(地址.IndexOf("杭州") > -1) exceptionRow["交货地"] = "杭州";
                        else if(地址.IndexOf("嘉定") > -1) exceptionRow["交货地"] = "嘉定";
                        else if(地址.IndexOf("松江") > -1) exceptionRow["交货地"] = "松江";
                        else if(地址.IndexOf("宝山") > -1) exceptionRow["交货地"] = "宝山";
                        else if(地址.IndexOf("无锡") > -1) exceptionRow["交货地"] = "无锡";
                
                        exceptionRow["客户产品代码"] = 商品货号;
                        //exceptionRow["雀巢产品代码"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["platform_product_code"].ToString() == 商品货号) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["platform_product_code"].ToString()  == 商品货号)["nestle_product_code"].ToString() : "";
                        exceptionRow["雀巢产品代码"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Customer_Material_No"].ToString() == 商品货号) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Customer_Material_No"].ToString()  == 商品货号)["Nestle_Material_No"].ToString() : "";
                        
                        exceptionRow["产品名称"] = 商品名称;    
                        exceptionRow["客户箱规"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Customer_Material_No"].ToString() == 商品货号) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Customer_Material_No"].ToString() == 商品货号)["Nestle_Case_Configuration"].ToString() : "";
                        exceptionRow["雀巢箱规"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Customer_Material_No"].ToString() == 商品货号) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Customer_Material_No"].ToString() == 商品货号)["Nestle_Case_Configuration"].ToString() : "";
                         
                        exceptionRow["BU"]="IB water";
                        exceptionRow["数量"] = Convert.ToInt32(总订个数);
                        exceptionRow["客户要求送货日"] = 退货截止日期;
                        exceptionRow["问题分类"] = "退货订单";
                        exceptionRow["问题详情描述"] = "订单为退货单";
                        ExceptionTable.Rows.Add(exceptionRow);
                        
                        DataRow orderItemRow = orderItemTable.NewRow();
                        orderItemRow["OrderNumber"] = 订单编号;
                        orderItemRow["ProductNumber"] = 商品货号;
                        orderItemRow["Barcode"] = 商品条码;
                        orderItemRow["ProductName"] = 商品名称;
                        orderItemRow["LineNumber"] = PO序号;
                        orderItemRow["Batch"] = 规格;
                        orderItemRow["Unit"] = 单位;
                        orderItemRow["Quantity"] = 0;
                        orderItemRow["TotalQuantity"] = Convert.ToInt32(总订个数);
                        orderItemTable.Rows.Add(orderItemRow);
                        
                        if(pdfArray[index + 4].IndexOf("备注") > -1){
                            nextTableData = false;
                        }
                        isFinishRow = false;
                    }
                    if(pdfItem == "N"){
                        tableDataIndex = 0;
                        PO序号++;
                    }
                    else tableDataIndex++;
                }
                index++;
            }
        }
        #endregion

}
//在这里编写您的函数或者类

public void fetchItemByLineSeperator(string pdfStr, string searchTerm, ref Dictionary<string, string> 主要信息)
{
    // Console.WriteLine(pdfStr);
    
    int 索引 = pdfStr.IndexOf(searchTerm);
    
    int 指定元素后换行符索引 = pdfStr.IndexOf("\r\n", 索引);
    // Console.WriteLine("searchTerm: {0}, 索引: {1}, 指定元素后换行符索引: {2}", searchTerm, 索引, 指定元素后换行符索引);
    主要信息[searchTerm] = pdfStr.Substring(索引+ searchTerm.Length, 指定元素后换行符索引 - 索引).Trim();
    // Console.WriteLine("指定值字符串: {0}", 指定值字符串);
}

public static int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
        //Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}