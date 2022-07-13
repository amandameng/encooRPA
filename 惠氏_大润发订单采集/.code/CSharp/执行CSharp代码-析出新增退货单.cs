//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    /* 以下是输出实际增量退货单*/
    DataTable existingReturnOrdersDT = (DataTable)existingReturnOrdersDic["existingReturnOrdersDT"];
    DataTable newReturnOrdersDTTmp = returnOrdersDT.Clone();
    foreach(DataRow dr in returnOrdersDT.Rows){
        string orderNumber = dr["采购单号"].ToString();
        DataRow[] drs = existingReturnOrdersDT.Select(string.Format("order_number = '{0}'", orderNumber));
        if(drs.Length == 0){
            newReturnOrdersDTTmp.ImportRow(dr);
        }
    }
    
    /* 以下是格式化订单跟db mapping列一致，以存进数据库*/
    if(newReturnOrdersDTTmp!= null && newReturnOrdersDTTmp.Rows.Count > 0){
        newReturnOrdersDTTmp.Columns["单品开票价"].ColumnName = "买价";
        DataTable uniqOrderNumDT = newReturnOrdersDTTmp.DefaultView.ToTable(true, new string[]{"采购单号"});
        
        AddMoreColumns(ref newReturnOrdersDTTmp);

        newReturnOrdersResultDT = newReturnOrdersDTTmp.Clone();
        foreach(DataRow dr in uniqOrderNumDT.Rows){
            string 采购单号 = dr["采购单号"].ToString();
            DataRow[] drs = newReturnOrdersDTTmp.Select(string.Format("采购单号 = '{0}'", 采购单号));
            decimal 大润发订单总金额 = 0m;
            List<DataRow> listRows = new List<DataRow>{};
            foreach(DataRow insideDR in drs){
                decimal 大润发总价 = toDecimalConvert(insideDR["买价"]) * toDecimalConvert(insideDR["已收货数量"]);
                insideDR["大润发总价"] = 大润发总价;
                insideDR["区域"] = dtRow_ModuleSettings["区域"];
                string dcNo = insideDR["门店"].ToString();
                insideDR["大仓号"] = splitDCInfo(dcNo);
                string customerSku = insideDR["货号"].ToString();
                string 惠氏产品码 = fetchWyethSku(customerSku);                
                string 惠氏订单编号 = fetchWyethOrderNum(采购单号, 惠氏产品码, dcNo);
                insideDR["惠氏订单编号"] = 惠氏订单编号;
                insideDR["客户名称"] = dtRow_ModuleSettings["customer_name"];

                大润发订单总金额 += 大润发总价;
            }
             foreach(DataRow insideDR in drs){
                insideDR["大润发订单总金额"] = 大润发订单总金额;
                newReturnOrdersResultDT.ImportRow(insideDR);
                if(!returnOrderNumList.Contains(采购单号)){
                    returnOrderNumList.Add(采购单号);
                }
            }
        }

       // printDT(newReturnOrdersResultDT);
    }
    

}
//在这里编写您的函数或者类

public string fetchWyethSku(string customerSku){
    string 惠氏产品码 = string.Empty;
    DataRow wyethSKUMappingRow = getWyethMappingRow(customerSku);
    if(wyethSKUMappingRow != null){
        惠氏产品码 = wyethSKUMappingRow["wyeth_material_no"].ToString();
    }
    return 惠氏产品码;
}

/// <summary>
/// 获取主数据匹配数据行
/// </summary>
/// <param name="customerSKU"></param>
/// <returns></returns>

public DataRow getWyethMappingRow(string customerSKU){
    DataTable materialMasterDataDT = (DataTable)dtRow_ModuleSettings["materialMasterDataDT"];
    DataRow[] drs = materialMasterDataDT.Select(string.Format("customer_material_no='{0}'", customerSKU));
    if(drs.Length > 0){
       return drs[0];
    }else{
        return null;
    }
}

/// <summary>
/// 生成惠氏订单号
/// </summary>
/// <param name="origOrderNum"></param>
/// <param name="惠氏产品码"></param>
/// <returns></returns>
public string fetchWyethOrderNum(string origOrderNum, string 惠氏产品码, string dcNo){
    string 惠氏订单编号 = string.Format("{0}.{1}", dcNo.Substring(1,3), origOrderNum);;
    if(string.IsNullOrEmpty(惠氏产品码)){
       return  惠氏订单编号;
    }
    string comment = specialProductComment(惠氏产品码);
    Console.WriteLine("comment: {0}", comment);
    if(!string.IsNullOrEmpty(comment)){
        if(comment.Contains("有机彩箱装")){
            惠氏订单编号 = 惠氏订单编号 + "-cxz1";
        }else if(comment.Contains("铂臻彩箱装")){
            惠氏订单编号 = 惠氏订单编号 + "-cxz2";
        }
    }
    return 惠氏订单编号;
}


/// <summary>
///  获取特殊品comment
/// </summary>
/// <param name="惠氏产品码"></param>
/// <returns></returns>
public string specialProductComment(string 惠氏产品码){
    string comment = string.Empty;
    DataTable specialProductsDT = (DataTable)dtRow_ModuleSettings["specialListDT"];
    // printDT(specialProductsDT);

    DataRow[] drs = specialProductsDT.Select(string.Format("sku_code='{0}'", 惠氏产品码));
    if(drs.Length > 0){
        comment = drs[0]["comment"].ToString();
    }
    return comment;
}

public void AddMoreColumns(ref DataTable targetDT){
    targetDT.Columns.Add("大润发总价", typeof(string));
    targetDT.Columns.Add("大润发订单总金额", typeof(string));
    targetDT.Columns.Add("区域", typeof(string));
    targetDT.Columns.Add("大仓号", typeof(string));
    targetDT.Columns.Add("惠氏订单编号", typeof(string));
    targetDT.Columns.Add("客户名称", typeof(string));
}

public string splitDCInfo(string 门店){
    string dcNo = 门店.Substring(0, 4);
    return dcNo;
}

public int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
       Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}

public decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
       Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

/// <summary>
/// 辅助打印数据表方法
/// </summary>
/// <param name="theDT"></param>
public void printDT(DataTable theDT){
    DataColumnCollection dcols = theDT.Columns;
    foreach(DataRow dr in theDT.Rows){
        foreach(DataColumn dc in dcols){
            Console.WriteLine("column:{0}, value:{1}", dc.ColumnName, dr[dc.ColumnName]);
        }
        Console.WriteLine("---------------------------------------");
    }
}