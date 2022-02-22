//代码执行入口，请勿修改或删除
public void Run()
{
    try{
        initOrdersDT();
        create_date_time = orderRow["Date"].ToString();
        location = orderRow["location"].ToString();
        orderLink = orderRow["order_link"].ToString();
        DataRow newOrderRow = orderDT.NewRow();
        parseOrder(ref newOrderRow);
        parseOrderItems(ref newOrderRow);
        
        // 判断山姆流程，产品是否不属于山姆主产品，如果是，order置空
        if(customer_name == "山姆" && 山姆水主产品数据 != null){
            List<string> 山姆水主产品list = 山姆水主产品数据.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["Customer_Material_No"].ToString()).ToList();
            List<string> 订单产品List = orderItemsDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["Item"].ToString()).ToList();
            IEnumerable<string> intersectLs = 山姆水主产品list.Intersect(订单产品List);
            if(intersectLs.Count() > 0){ // 是山姆水产品
                foreach(DataRow samWaterDr in orderDT.Rows){
                    samWaterDr["customer_name"] = 山姆水客户名;
                }
                foreach(DataRow samWaterItemDr in orderItemsDT.Rows){
                    samWaterItemDr["customer_name"] = 山姆水客户名;
                }
            }
        }        
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
        "total_order_amount_after_adjustments", "total_line_items", "total_units_ordered", "customer_name", "file_path"};
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
        // orderItemsDT.Rows.Add(OrderItemDRow);
    
        if(rowIndex== 1)
        {
            orderItemsDT.Columns.Add("itemDescription", typeof(string));
            orderItemsDT.Columns.Add("taxType", typeof(string));
            orderItemsDT.Columns.Add("taxPercent", typeof(string));
    
            orderItemsDT.Columns.Add("allowance", typeof(string));
            orderItemsDT.Columns.Add("allowanceDecp", typeof(string));
            orderItemsDT.Columns.Add("allowanceQty", typeof(string));
            orderItemsDT.Columns.Add("allowanceUOM", typeof(string));
            orderItemsDT.Columns.Add("allowancePercent", typeof(string));
            orderItemsDT.Columns.Add("allowanceTotal", typeof(string));
            orderItemsDT.Columns.Add("itemInstructions", typeof(string));
    
            orderItemsDT.Columns["allowance"].DefaultValue = null;
            orderItemsDT.Columns["allowanceDecp"].DefaultValue = null;
            orderItemsDT.Columns["allowanceQty"].DefaultValue = null;
            orderItemsDT.Columns["allowanceUOM"].DefaultValue = null;
            orderItemsDT.Columns["allowancePercent"].DefaultValue = null;
            orderItemsDT.Columns["allowanceTotal"].DefaultValue = null;
            orderItemsDT.Columns["itemInstructions"].DefaultValue = null;
            
            orderItemsDT.Columns.Add("orderNumber", typeof(string));
            orderItemsDT.Columns.Add("document_link", typeof(string));
            orderItemsDT.Columns.Add("customer_name", typeof(string));
        }
        string detailBodyId = rowIndex.ToString().PadLeft(3, '0') + "lineDetailbody";
    
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
        OrderItemDRow["customer_name"] = customer_name;

        orderItemsDT.Rows.Add(OrderItemDRow);
        rowIndex += 1;
    }
     // Clean useless columns
    DataColumn dcol = orderItemsDT.Columns["Column1"];
    orderItemsDT.Columns.Remove(dcol);
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
    newOrderRow["order_number"] = poNumber;
    newOrderRow["order_type"] = orderType;
    newOrderRow["create_date"] = Convert.ToDateTime(poOrderDate);
    newOrderRow["location"] = location.Trim();
    newOrderRow["customer_name"] = customer_name;
    newOrderRow["create_date_time"] = create_date_time;
    newOrderRow["document_link"] = orderLink;
    newOrderRow["ship_date"] = Convert.ToDateTime(shipDate);
    newOrderRow["must_arrived_by"] = Convert.ToDateTime(mustArriveBy);
    newOrderRow["promotional_event"] = promotionalEvent; 
}