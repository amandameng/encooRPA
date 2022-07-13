//代码执行入口，请勿修改或删除
public void Run()
{
    try{
        initOrdersDT();
        create_date_time = orderRow["Date"].ToString();
        location = orderRow["location"].ToString().Trim();
        orderLink = orderRow["order_link"].ToString();
        DataRow newOrderRow = orderDT.NewRow();
        parseOrder(ref newOrderRow);
        parseOrderItems(ref newOrderRow);
              
    }catch(Exception e){
        Console.WriteLine(e);
        throw new Exception("订单抓取失败！请查看是否是网页结构发生变化");
    }

}
//在这里编写您的函数或者类

public void initOrdersDT(){
    orderDT = new DataTable();
    List<string> orderColumns = new List<string>{"order_number", "order_type", "create_date", "create_date_time", "document_link", "ship_date",
        "must_arrived_by", "promotional_event", "location", "allowance_or_charge", "allowance_description", "allowance_percent", "allowance_total",
        "total_order_amount_after_adjustments", "total_line_items", "total_units_ordered", "平台商", "file_path"};
    foreach(string item in orderColumns){
       orderDT.Columns.Add(item, typeof(string));
    }
}
    
    
// Order Item Info
public void parseOrderItems(ref DataRow newOrderRow){
    orderItemsDT = new DataTable();
    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
    doc.LoadHtml(orderItemHtmlCode);
    // td[text()="Supplier Stock #"]/parent::*
    var headers = doc.DocumentNode.SelectNodes("//tr[not(contains(@id, \"lineDetailbody\"))]/td[text()=\"Supplier Stock #\"]/parent::*/td");
    var lineItems = doc.DocumentNode.SelectNodes("//tr[contains(@id, \"lineDetailheader\")]");

    foreach (HtmlNode header in headers){
        orderItemsDT.Columns.Add(header.InnerText.Trim());
    }
    int rowIndex = 1;
    foreach (HtmlNode row in lineItems)
    {
        DataRow OrderItemDRow = orderItemsDT.NewRow();
        List<string> ItemList = row.SelectNodes("td").Select(td => td.InnerText.Trim()).ToList();
        string lineNumber = ItemList[1];
       
        if(rowIndex== 1)
        {
            orderItemsDT.Columns.Add("itemDescription", typeof(string));
            orderItemsDT.Columns.Add("taxType", typeof(string));
            orderItemsDT.Columns.Add("taxPercent", typeof(string));
    
            orderItemsDT.Columns.Add("item_allowance_or_charge", typeof(string));
            orderItemsDT.Columns.Add("item_allowance_description", typeof(string));
            orderItemsDT.Columns.Add("item_allowance_qty", typeof(string));
            orderItemsDT.Columns.Add("item_allowance_uom", typeof(string));
            orderItemsDT.Columns.Add("item_allowance_percent", typeof(string));
            orderItemsDT.Columns.Add("item_allowance_total", typeof(string));
            orderItemsDT.Columns.Add("item_instructions", typeof(string));
    
            orderItemsDT.Columns["item_allowance_or_charge"].DefaultValue = null;
            orderItemsDT.Columns["item_allowance_description"].DefaultValue = null;
            orderItemsDT.Columns["item_allowance_qty"].DefaultValue = null;
            orderItemsDT.Columns["item_allowance_uom"].DefaultValue = null;
            orderItemsDT.Columns["item_allowance_percent"].DefaultValue = null;
            orderItemsDT.Columns["item_allowance_total"].DefaultValue = null;
            orderItemsDT.Columns["item_instructions"].DefaultValue = null;
            
            orderItemsDT.Columns.Add("orderNumber", typeof(string));
            orderItemsDT.Columns.Add("document_link", typeof(string));
            orderItemsDT.Columns.Add("惠氏订单号", typeof(string));
        }
        string detailBodyId = lineNumber + "lineDetailbody";
        // string detailBodyId = rowIndex.ToString().PadLeft(3, '0') + "lineDetailbody";

        // HtmlNode itemBodyNode = doc.DocumentNode.SelectSingleNode();
        string detailBodyNode = String.Format("//tr[@id=\"{0}\"]", detailBodyId);
        HtmlNode itemDescription = doc.DocumentNode.SelectSingleNode(String.Format("{0}//table//td[text()=\"Item Description\"]/following-sibling::td", detailBodyNode));
        if(itemDescription != null)
        {
            ItemList.Add(itemDescription.InnerText.Trim());
            // OrderItemDRow["itemDescription"] = itemDescription.InnerText;
     
        }
        else
        {
            foreach (int i in new Int32[] { 1 })
            {
                ItemList.Add(null);
            }
        }
    
    
        var itemTaxNodes = doc.DocumentNode.SelectNodes(String.Format("{0}//table//td[text()=\"Tax Type\"]/parent::tr/following-sibling::tr/td", detailBodyNode));
        if (itemTaxNodes != null)
        {
            ItemList.Add(itemTaxNodes[0].InnerText.Trim());
            ItemList.Add(itemTaxNodes[1].InnerText.Trim());
            //OrderItemDRow["taxType"] = itemTaxNodes[0].InnerText;
            // OrderItemDRow["taxPercent"] = itemTaxNodes[1].InnerText;
     
        }
        else
        {
            foreach (int i in new Int32[] { 1, 2 })
            {
                ItemList.Add(null);
            }
        }
    
        var itemAllowanceNodes = doc.DocumentNode.SelectNodes(String.Format("{0}//table//td[text()=\"Allowance / Charge\"]/parent::tr/parent::tbody/tr", detailBodyNode));
        if (itemAllowanceNodes != null)
        {
            string[] allowanceValuesArr = itemAllowanceNodes[1].SelectNodes("td").Select(td => td.InnerText.Trim()).ToArray();
            foreach (string allowanceValue in allowanceValuesArr)
                ItemList.Add(allowanceValue.Replace("(", "").Replace(")", ""));
        }
        else
        {
            foreach(int i in new Int32[] { 1, 2, 3, 4, 5, 6 })
            {
                ItemList.Add(null);
            }
                
        }
    
        HtmlNode itemInstructions = doc.DocumentNode.SelectSingleNode(String.Format("{0}//table//td[contains(text(),\"Item Instructions\")]/following-sibling::td", detailBodyNode));
        if (itemInstructions != null)
        {
            ItemList.Add(itemInstructions.InnerText.Trim());
            // OrderItemDRow["itemDescription"] = itemDescription.InnerText;
        }
        else
        {
            foreach (int i in new Int32[] { 1})
            {
                ItemList.Add(null);
            }
        }
    
        OrderItemDRow.ItemArray = ItemList.ToArray();
        OrderItemDRow["orderNumber"] = newOrderRow["order_number"];
        OrderItemDRow["document_link"] = orderLink;
        string wyeth_poid = generateWyethPOID(OrderItemDRow);
        OrderItemDRow["惠氏订单号"] = wyeth_poid;

        orderItemsDT.Rows.Add(OrderItemDRow);
        rowIndex += 1;
    }
     // Clean useless columns
    DataColumn dcol = orderItemsDT.Columns["Column1"];
    orderItemsDT.Columns.Remove(dcol);
}
public string generateWyethPOID(DataRow OrderItemDRow){
    string wyethPOID = OrderItemDRow["orderNumber"].ToString();
    string  product_code = OrderItemDRow["Item"].ToString();
    DataRow[] mmDRs = materialMasterDataDT.Select(string.Format("customer_material_no = '{0}'", product_code));
    if(mmDRs.Length > 0){
        DataRow mmDR = mmDRs[0];
        string wyethSku = mmDR["wyeth_material_no"].ToString();
        string soldTo = getSoldTo(location);
        wyethPOID = GenerateWyethPOID(wyethPOID, soldTo, wyethSku, product_code, location);
    }
    return wyethPOID;
}

public string getSoldTo(string location){
    string soldTo = string.Empty;
    DataRow[] soldToShipToDrs = curShipToDT.Select(string.Format("DC编号 = '{0}'", location));
    if(soldToShipToDrs.Length > 0){
        soldTo = soldToShipToDrs[0]["Sold to"].ToString();
    }
    return soldTo;
}

public string GenerateWyethPOID(string wyethPOID, string soldTo, string wyethSku, string customerSkuCode, string dcNo){
        string comment = string.Empty;
        
        DataRow[] specialDRInDC = specialListDT.Select(string.Format("sold_to='{0}' and sku_code = '{1}' and customer_sku_code='{2}' and dc_no = '{3}'", soldTo, wyethSku, customerSkuCode, dcNo));
        if(specialDRInDC.Length > 0){
            comment = specialDRInDC[0]["comment"].ToString();
        }else{
            DataRow[] specialDR = specialListDT.Select(string.Format("sold_to='{0}' and sku_code = '{1}' and customer_sku_code='{2}'", soldTo, wyethSku, customerSkuCode));
            if(specialDR.Length > 0){
                comment = specialDR[0]["comment"].ToString();
            }
        }
        
        if(!string.IsNullOrEmpty(comment)){
            string[] commentArr = comment.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
            List<string> commentList = commentArr.ToList();
            if(commentList.Contains("整箱")){
                wyethPOID = wyethPOID + "-zx";
            }
           if(commentList.Contains("彩箱装")){
                wyethPOID = wyethPOID + "-cxz";
            }
           if(commentList.Contains("CVP")){
                wyethPOID = wyethPOID + "-CVP";
            }
        }
        return wyethPOID;
}


// Order info
public void parseOrder(ref DataRow newOrderRow){
   orderBasicInfo(ref newOrderRow);
   parseOrderTotalInfo(ref newOrderRow);
   orderDT.Rows.Add(newOrderRow);
}

public void parseOrderTotalInfo(ref DataRow newOrderRow){
    // Allowance Rwows
   
    DataRow[] allowanceRows = orderAllowanceDT.Select("`Column-0`='Allowance'");
    string allowanceOrCharge = string.Empty;
    string allowanceDescription = string.Empty;
    string allowancePercent = string.Empty;
    string allowanceTotal = string.Empty;
    
    foreach(DataRow dr in allowanceRows){
        string item1 = dr[0].ToString();
        string item2 = dr[1].ToString();
        string item3 = dr[2].ToString();
        string item4 = dr[3].ToString();
        allowanceOrCharge = String.IsNullOrEmpty(allowanceOrCharge) ? item1 : allowanceOrCharge + "，" + item1;
        allowanceDescription = String.IsNullOrEmpty(allowanceDescription) ? item2 : allowanceDescription + "，" + item2;
        allowancePercent = String.IsNullOrEmpty(allowancePercent) ? item3 : allowancePercent + "，" + item3;
        allowanceTotal = String.IsNullOrEmpty(allowanceTotal) ? item4 : allowanceTotal + "，" + item4;
    }
    
    DataRow toamountRow = orderAllowanceDT.Select("`Column-0` = 'Total Order Amount (After Adjustments)'")[0];
    string toamount = toamountRow != null ? toamountRow[3].ToString() : "";
    DataRow totalLineItemRow = orderAllowanceDT.Select("`Column-0` = 'Total Line Items'")[0];
    string totalLineItems = totalLineItemRow != null ? totalLineItemRow[3].ToString() : "";
    DataRow totalUnitsOrderedRow = orderAllowanceDT.Select("`Column-0` = 'Total Units Ordered'")[0];
    string totalUnitsOrdered = totalUnitsOrderedRow !=null ? totalUnitsOrderedRow[3].ToString() : "";
    
    newOrderRow["allowance_or_charge"] = allowanceOrCharge;
    newOrderRow["allowance_description"] = allowanceDescription;
    newOrderRow["allowance_percent"] = allowancePercent;
    newOrderRow["allowance_total"] = allowanceTotal.Replace("(", "").Replace(")", "");
    newOrderRow["total_order_amount_after_adjustments"] = toamount;
    newOrderRow["total_line_items"] = totalLineItems;
    newOrderRow["total_units_ordered"] = totalUnitsOrdered;
}

public void orderBasicInfo(ref DataRow newOrderRow){
    string poNumber = orderBasicDT.Select(String.Format("label = '{0}'", "Purchase Order Number"))[0]["value"].ToString().Trim();
    string poOrderDate = orderBasicDT.Select(String.Format("label = '{0}'", "Purchase Order Date"))[0]["value"].ToString().Trim();
    // [Ship Not Before] should be equal to [Ship No Later Than]
    string shipDate = orderBasicDT.Select(String.Format("label = '{0}'", "Ship Not Before"))[0]["value"].ToString().Trim();
    string shipNotLaterThan = orderBasicDT.Select(String.Format("label = '{0}'", "Ship No Later Than"))[0]["value"].ToString().Trim();
    string mustArriveBy = orderBasicDT.Select(String.Format("label = '{0}'", "Must Arrive By"))[0]["value"].ToString().Trim();
    /*
      SAM 产品
      Ship Data = Ship Not Before = Ship No Later Than
      Must Arrive By = Cancel Date
    */
    if(String.IsNullOrEmpty(shipDate)){
        shipDate = orderBasicDT.Select(String.Format("label = '{0}'", "Ship Date"))[0]["value"].ToString().Trim();
    }
    if(String.IsNullOrEmpty(mustArriveBy)){
        mustArriveBy = orderBasicDT.Select(String.Format("label = '{0}'", "Cancel Date"))[0]["value"].ToString().Trim();
    }

    string orderType = additionalDetailsDT.Select(String.Format("label = '{0}'", "Order Type"))[0]["value"].ToString().Trim();
    string promotionalEvent = additionalDetailsDT.Select(String.Format("label = '{0}'", "Promotional Event"))[0]["value"].ToString().Trim();
    
    string supplierNo = supplierDT.Select(String.Format("label = '{0}'", "Supplier Number"))[0]["value"].ToString().Trim();
    // 196654640 是山姆，196654260是大润发
    realCustomerName = customer_name;
    if(supplierNo == "196654640"){
        realCustomerName = "山姆";
    }else if(supplierNo == "196654260"){
        realCustomerName = "沃尔玛";
    }

    newOrderRow["order_number"] = poNumber;
    newOrderRow["order_type"] = orderType;
    newOrderRow["create_date"] = Convert.ToDateTime(poOrderDate);
    newOrderRow["location"] = location;
    newOrderRow["平台商"] = realCustomerName;

    newOrderRow["create_date_time"] = create_date_time;
    newOrderRow["document_link"] = orderLink;
    newOrderRow["ship_date"] = Convert.ToDateTime(shipDate);
    newOrderRow["must_arrived_by"] = Convert.ToDateTime(mustArriveBy);
    newOrderRow["promotional_event"] = promotionalEvent; 
}