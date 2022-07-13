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
        
        #region 写入Excel To 数据表    
        if(pdfStr.IndexOf("预定交货日") > -1){
            int index = 0;
            bool nextTableData = false;
            int tableDataIndex = 0;
            
            string 订单类型 = "订货单";
            string 订单编号 = "";
            
            Dictionary<string, string> 主要信息 = new Dictionary<string, string> { };
            
            fetchItemByLineSeperator(rawPdfstr, 订货日字符, ref 主要信息);
            string 订货日 = Regex.Split(主要信息[订货日字符], @"\s+", RegexOptions.IgnorePatternWhitespace)[0];

            fetchItemByLineSeperator(rawPdfstr, 预定交货日字符, ref 主要信息);
            string 预定交货日 = Regex.Split(主要信息[预定交货日字符], @"\s+", RegexOptions.IgnorePatternWhitespace)[0];
    
            fetchItemByLineSeperator(rawPdfstr, 地址字符, ref 主要信息);
            string 地址 = 主要信息[地址字符];
            fetchItemByLineSeperator(rawPdfstr, fromStr, ref 主要信息);
            string from = 主要信息[fromStr];
            string[] fromArr = Regex.Split(from, @"\s+", RegexOptions.IgnorePatternWhitespace);
            string WMDCCode = fromArr[0].Trim();
            Console.WriteLine("WMDCCode ------{0}", WMDCCode);

            string 送货店 = fromArr[1];
            string soldToCode = string.Empty;
            string shipToCode = string.Empty;
            
            /*
            bool 第二個元素是第 = pdfArray[2] == "第"; // 一種模板第二個元素是 ”第“， 其餘的是 “订货日” 開始
            string 订货日 =  第二個元素是第  ? pdfArray[24] : pdfArray[4] ;
            string 预定交货日 = 第二個元素是第 ?  (pdfArray[26] == "0" ?  pdfArray[30] : pdfArray[28])  :  (pdfArray[6] == "0" ? pdfArray[10] : pdfArray[8]) ;
            
            string 地址 = 第二個元素是第 ? (pdfArray[26] == "0" ?  pdfArray[56] : pdfArray[52]) : (pdfArray[6] == "0" ? pdfArray[56] : pdfArray[52]) ;
            string 送货店 = 第二個元素是第 ? (pdfArray[26] == "0" ? pdfArray[48] : pdfArray[44]) : (pdfArray[6] == "0" ? pdfArray[48] : pdfArray[44]);
            */
            string 备注 = string.Format("{0} {1} {2}",pdfArray[pdfArray.Length - 5], pdfArray[pdfArray.Length - 3], pdfArray[pdfArray.Length - 1]);
            Console.WriteLine("---订货日{0}-预定交货日{1}--地址{2}--送货店{3}--备注{4} ", 订货日, 预定交货日, 地址, 送货店, 备注);

            //创建Order Table Row
            DataRow orderRow = ordersTable.NewRow();
            orderRow["OrderType"] = 订单类型;
            orderRow["OrderDate"] = Convert.ToDateTime(订货日);               
            orderRow["ShipDate"] = Convert.ToDateTime(预定交货日);
            orderRow["ShipAddress"] = 地址;
            orderRow["WMDC"] = WMDCCode;
            orderRow["ShipFrom"] = 送货店;
            
            string 商品货号 = "";
            string 商品名称 = "";
            string 品牌 = "";
            string 规格 = "";
            string 单位 = "";
            int 订购倍数 = 0;
            int 总订购倍数 = 0;
            string 总订个数 = "";
            string 商品条码 = "";
            string 产地 = "";
            string QS = "";
            int PO序号 = 1;
            string 雀巢编码 = "";
            bool 雀巢码检查 = true;
            
            bool skipFlag = false;
            foreach(string pdfItem in pdfArray){
                if(pdfItem.IndexOf("订单编号") > -1){
                    订单编号 = pdfArray[index + 2];
                    Console.WriteLine("index:{0}, 订单编号:{1}", index, 订单编号);
                    orderRow["OrderNumber"] = 订单编号;
                }
                
                if(pdfItem == "金额"){
                    nextTableData = true;
                }
                if(pdfItem.IndexOf("备注") > -1){
                    if(!skipFlag) skipFlag = true;
                    else break;
                }
                else if(nextTableData && pdfItem != "金额"){
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
                        case 11 : //订购倍数
                            订购倍数 = toIntConvert(pdfItem.Trim());
                            break;
                        case 13 : //总订个数
                            总订个数 = pdfItem.Trim().Replace(",","").Replace("-","");
                            break;
                        case 15 : //商品条码
                            商品条码 = pdfItem;
                            break;
                        case 17 : //产地
                            产地 = pdfItem;
                            break;
                        case 19 : //QS
                            QS = pdfItem;
                            break;
                        default:
                            break;
                    };
                    if(tableDataIndex == 19) {
    
                        //写入Excel To
                        DataRow excelRow = ExcelToTable.NewRow();
                        excelRow["OrderDate"] = Convert.ToDateTime(订货日);  // 不进入excel文件
                        excelRow["OrderType"] = "OR";
                        excelRow["SalesOrg"] = "CN26"; 
                        excelRow["DistributionChannel"] = "01";


                        foreach(DataRow dRow in 仓库信息表.Rows){
                            string Nestle_Plant_No = dRow["Nestle_Plant_No"].ToString();
                            if(WMDCCode == Nestle_Plant_No){
                                soldToCode = dRow["Sold_to_Code"].ToString();
                                shipToCode = dRow["Ship_to_Code"].ToString();
                            }
                        }
                        excelRow["SoldTo"] = soldToCode;
                        excelRow["ShipTo"] = shipToCode;
                        /*
                        if(地址.IndexOf("杭州") > -1) excelRow["SoldTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "杭州") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "杭州")["Sold_to_Code"].ToString() : "";
                        else if(地址.IndexOf("无锡") > -1) excelRow["SoldTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "无锡") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "无锡")["Sold_to_Code"].ToString() : "";
                        else excelRow["SoldTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "嘉定") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "嘉定")["Sold_to_Code"].ToString() : "";
                        
                        if(地址.IndexOf("杭州") > -1) excelRow["ShipTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "杭州") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "杭州")["Ship_to_Code"].ToString() : "";
                        else if(地址.IndexOf("嘉定") > -1) excelRow["ShipTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "嘉定") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "嘉定")["Ship_to_Code"].ToString() : "";
                        else if(地址.IndexOf("松江") > -1) excelRow["ShipTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "松江") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "松江")["Ship_to_Code"].ToString() : "";
                        else if(地址.IndexOf("宝山") > -1) excelRow["ShipTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "宝山") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "宝山")["Ship_to_Code"].ToString() : "";
                        else if(地址.IndexOf("无锡") > -1) excelRow["ShipTo"] = 仓库信息表.AsEnumerable().Cast<DataRow>().Any(s => s["Region"].ToString() == "无锡") ? 仓库信息表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Region"].ToString() == "无锡")["Ship_to_Code"].ToString() : "";
                        else{
                            // throw(new Exception("订单地址不在设定的范围【无锡，嘉定，杭州，宝山，松江】"));

                        }
                        */
                        
                        excelRow["POOrder"] = PO序号.ToString();
                        excelRow["PONumber"] = "IBU" + 订单编号;
                        excelRow["CustomerOrderNumber"] = 订单编号; // 不进入excel文件
                        excelRow["DeliveryDate"] = Convert.ToDateTime(预定交货日).ToString("yyyyMMdd");
                        //excelRow["MaterialCode"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["platform_product_code"].ToString() == 商品货号) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["platform_product_code"].ToString() == 商品货号)["nestle_product_code"].ToString() : "";
                        excelRow["MaterialCode"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Customer_Material_No"].ToString() == 商品货号) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Customer_Material_No"].ToString() == 商品货号)["Nestle_Material_No"].ToString() : "";
                        雀巢编码 = excelRow["MaterialCode"].ToString();
                        if(string.IsNullOrEmpty(雀巢编码)){
                            雀巢码检查 = false;
                        }
                        excelRow["Qty"] = 订购倍数;
                        总订购倍数 += 订购倍数;
                        excelRow["UoM"] = "CS";
                        ExcelToTable.Rows.Add(excelRow);   
                        
                        DataRow orderItemRow = orderItemTable.NewRow();
                        orderItemRow["OrderNumber"] = 订单编号;
                        orderItemRow["ProductNumber"] = 商品货号;
                        orderItemRow["Barcode"] = 商品条码;
                        orderItemRow["ProductName"] = 商品名称;
                        orderItemRow["LineNumber"] = PO序号;
                        orderItemRow["Batch"] = 规格;
                        orderItemRow["Unit"] = 单位;
                        orderItemRow["Quantity"] = 订购倍数;
                        orderItemRow["TotalQuantity"] = Convert.ToInt32(总订个数);
                        orderItemTable.Rows.Add(orderItemRow);
                        
                        tableDataIndex = 0;
                        PO序号++;
                        if(pdfArray[index + 4].IndexOf("备注") > -1){
                            nextTableData = false;
                        }
                    }
                    else tableDataIndex++;
                }
                index++;
            }
            #endregion
            
        #region 合规性检查
            
            bool 起送量检查 = true;
            bool 交货日检查 = true;
            string 错误信息 = "";
            //起送量检查
            int 订单起送量 = 0;
            DateTime 预定交货日日期;
            bool dateValid = DateTime.TryParse(预定交货日, out 预定交货日日期);
            // 日期不合法
            if(!dateValid){
                错误信息 = "RDD不合法";
            }else{
                if(地址.IndexOf("无锡") > -1 || 地址.IndexOf("杭州") > -1){
                    // 订单起送量 = orderItemTable.AsEnumerable().Cast<DataRow>().Sum(s => Convert.ToInt32(s["Quantity"].ToString()));
                    // !!! 订单起送量 = 总订购倍数
                    Console.WriteLine("===总订购倍数==={0}", 总订购倍数);
                    if(总订购倍数 < 80){
                        起送量检查 = false;
                        错误信息 = "市区外起送量不足80cs";
                    }
                }
                else{
                    if(预定交货日日期.Month > 10 || 预定交货日日期.Month < 3){  //淡季
                        // 订单起送量 = orderItemTable.AsEnumerable().Cast<DataRow>().Sum(s => Convert.ToInt32(s["Quantity"].ToString()));
                        if(总订购倍数 < 10){
                            起送量检查 = false;
                            错误信息 = "淡季市内起送量不足10cs";
                        }
                    }
                    else{ //旺季
                        // 订单起送量 = orderItemTable.AsEnumerable().Cast<DataRow>().Sum(s => Convert.ToInt32(s["Quantity"].ToString()));
                        if(总订购倍数 < 20){
                            起送量检查 = false;
                            错误信息 = "旺季市内起送量不足20cs";
                        }
                    }
                }
                orderRow["TotalUnit"] = 总订购倍数; // 订单起送量 => 总订购倍数
                ordersTable.Rows.Add(orderRow);
                
                // 交货日检查
                // 【读单日期】跟【预定交货日】比较
                string timeNowDateStr = DateTime.Now.ToString("yyyy/MM/dd");
    
                if(地址.IndexOf("无锡") > -1 || 地址.IndexOf("杭州") > -1){
                    if(Convert.ToDateTime(timeNowDateStr).AddDays(2) > 预定交货日日期){
                        交货日检查 = false;
                        if(string.IsNullOrEmpty(错误信息)) 错误信息 = "市外订单，RDD不足Day+2";
                        else 错误信息 += @"/市外订单，RDD不足Day+2";
                    }
                }
                else{
                    if(Convert.ToDateTime(timeNowDateStr).AddDays(1) > 预定交货日日期){
                        交货日检查 = false;
                        if(string.IsNullOrEmpty(错误信息)) 错误信息 = "市外订单，RDD不足Day+1";
                        else 错误信息 += @"/市内订单，RDD不足Day+1";
                    }
                }                
            }

            
            //雀巢产品码检查
  /*          if (!雀巢码检查)
            {
                 if(string.IsNullOrEmpty(错误信息)) 错误信息 = "订单无法获取雀巢编码";
                 else 错误信息 += @"/订单无法获取雀巢编码";
            }
*/
            if(string.IsNullOrEmpty(soldToCode.ToString())){
                string errorMsg = "Sold To 为空";
                错误信息 = string.IsNullOrEmpty(错误信息) ? errorMsg : 错误信息 + @"/" + errorMsg;
            }

            if(string.IsNullOrEmpty(shipToCode.ToString())){
                string errorMsg = "Ship To 为空";
                错误信息 = string.IsNullOrEmpty(错误信息) ? errorMsg : 错误信息 + @"/" + errorMsg;
            }
            #endregion
            
        #region 写入Clean 或者 Exception 数据表
            if(!string.IsNullOrEmpty(错误信息)){ //写入Exception表  
                foreach(DataRow excelItem in ExcelToTable.Rows){           
                     if ((excelItem["PONumber"]).ToString() == "IBU" + 订单编号)
                        {
                            DataRow exceptionRow = ExceptionTable.NewRow();              
                            exceptionRow["渠道"] = "IB";
                            exceptionRow["客户名称"] = "全家";
                            exceptionRow["订单日期"] =  Convert.ToDateTime(订货日).ToString("yyyy/MM/dd");
                            exceptionRow["客户PO"] = 订单编号; 
                           
                            //exceptionRow["客户产品代码"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["雀巢码"].ToString() == excelItem["MaterialCode"].ToString()) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["雀巢码"].ToString() == excelItem["MaterialCode"].ToString())["客户码"].ToString() : "";       
                            //exceptionRow["雀巢产品代码"] = excelItem["MaterialCode"].ToString();        
                            //exceptionRow["产品名称"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["雀巢码"].ToString() == excelItem["MaterialCode"].ToString()) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["雀巢码"].ToString() == excelItem["MaterialCode"].ToString())["产品描述"].ToString() : "";
                            
                            exceptionRow["客户产品代码"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString()) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString())["Customer_Material_No"].ToString() : "";       
                            exceptionRow["雀巢产品代码"] = excelItem["MaterialCode"].ToString();        
                            exceptionRow["产品名称"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString()) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString())["Material_Description"].ToString() : "";
                            
                            exceptionRow["客户箱规"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString()) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString())["Nestle_Case_Configuration"].ToString() : "";
                            exceptionRow["雀巢箱规"] = 产品关系表.AsEnumerable().Cast<DataRow>().Any(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString()) ? 产品关系表.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Nestle_Material_No"].ToString() == excelItem["MaterialCode"].ToString())["Nestle_Case_Configuration"].ToString() : "";
                            
                            exceptionRow["BU"]="IB water";
                            exceptionRow["数量"] = excelItem["Qty"].ToString();
                            exceptionRow["雀巢SAP_PO"] = "IBU" + 订单编号;            
                
                            exceptionRow["客户要求送货日"] = dateValid ? 预定交货日日期.ToString("yyyy/MM/dd") : "";
                            if(地址.IndexOf("杭州") > -1) exceptionRow["交货地"] = "杭州";
                            else if(地址.IndexOf("嘉定") > -1) exceptionRow["交货地"] = "嘉定";
                            else if(地址.IndexOf("松江") > -1) exceptionRow["交货地"] = "松江";
                            else if(地址.IndexOf("宝山") > -1) exceptionRow["交货地"] = "宝山";
                            else if(地址.IndexOf("无锡") > -1) exceptionRow["交货地"] = "无锡";
                            
                            if(!起送量检查 && !交货日检查){
                                exceptionRow["问题分类"] = "订单不满起送量/RDD问题";
                            }
                            else if(!起送量检查){
                                exceptionRow["问题分类"] = "订单不满起送量";
                            }
                            else if(!交货日检查){
                                exceptionRow["问题分类"] = "RDD问题";
                            }
                            //else if(!交货日检查){
                            //    if(地址.IndexOf("无锡") > -1 || 地址.IndexOf("杭州") > -1){
                            //        exceptionRow["问题分类"] = "市外RDD不足";
                            //    }
                            //     else{
                            //        exceptionRow["问题分类"] = "市内RDD不足";
                            //    }
                            //}
                            if(string.IsNullOrEmpty(exceptionRow["雀巢产品代码"].ToString())){
                                string errorMsg = "无法mapping雀巢产品";
                                exceptionRow["问题分类"] = exceptionRow["问题分类"].ToString() + @"/" + errorMsg;
                                exceptionRow["问题详情描述"] = string.IsNullOrEmpty(错误信息) ? errorMsg : 错误信息 + @"/" + errorMsg;
                            }else{
                                exceptionRow["问题详情描述"] = 错误信息;
                            }
                            
                            if(Convert.ToInt32(excelItem["Qty"]) == 0){
                                string errorMsg = "Qty为0";
                                exceptionRow["问题分类"] = exceptionRow["问题分类"].ToString() + @"/" + errorMsg;
                                exceptionRow["问题详情描述"] = string.IsNullOrEmpty(错误信息) ? errorMsg : 错误信息 + @"/" + errorMsg;
                            }else{
                                exceptionRow["问题详情描述"] = 错误信息;
                            }
                            
                            ExceptionTable.Rows.Add(exceptionRow);
                      }
                }
            }
            else{ //写入Clean表           
                DataRow cleanRow = CleanTable.NewRow();
                cleanRow["渠道"] = "IB";
                cleanRow["读单日期"] = (DateTime.Now).ToString("yyyy/MM/dd");
                cleanRow["客户名称"] = "全家";
                cleanRow["雀巢PO_No"] = "IBU" + 订单编号;
                cleanRow["客户PO_NO"] = 订单编号;
                cleanRow["订单数量"] = 总订购倍数;
                cleanRow["要求送货日"] = 预定交货日日期.ToString("yyyy/MM/dd");
                if(地址.IndexOf("杭州") > -1) cleanRow["交货地"] = "杭州";
                else if(地址.IndexOf("嘉定") > -1) cleanRow["交货地"] = "嘉定";
                else if(地址.IndexOf("松江") > -1) cleanRow["交货地"] = "松江";
                else if(地址.IndexOf("宝山") > -1) cleanRow["交货地"] = "宝山";
                else if(地址.IndexOf("无锡") > -1) cleanRow["交货地"] = "无锡";
                cleanRow["备注"] = 备注;
                CleanTable.Rows.Add(cleanRow);
            }       
        }
        #endregion
        
        #region 退货单处理
        else if(pdfStr.IndexOf("预订退货日") > -1){
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
                            总订个数 = pdfItem.Trim().Replace(",","");
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
                        int 总订个数整数 = Convert.ToInt32(总订个数);
                        exceptionRow["数量"] = 总订个数整数;
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
                        orderItemRow["TotalQuantity"] = Convert.ToInt32(总订个数整数);
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