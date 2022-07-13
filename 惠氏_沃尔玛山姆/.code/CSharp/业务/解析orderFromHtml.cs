//代码执行入口，请勿修改或删除
public void Run(){
    // DataTable
    // orderDT 加到 增量订单数据表
    foreach(DataRow excelDr in validOrdersFromListingDT.Rows){
        DataTable orderDT = new DataTable();
        DataTable orderItemsDT = new DataTable();
        WalmartOrderHTMLParser walmartParser = new WalmartOrderHTMLParser(excelDr, customer_name, 上传文件解压缩路径);
        walmartParser.parseHtml(ref orderDT, ref orderItemsDT);
        
        
        if(增量订单数据表 == null || 增量订单数据表.Rows.Count == 0){
            增量订单数据表 = orderDT;
        }else{
            增量订单数据表.Merge(orderDT);
        }
        if(增量订单详情数据表 == null || 增量订单详情数据表.Rows.Count == 0){
            增量订单详情数据表 = orderItemsDT;
        }else{
           增量订单详情数据表.Merge(orderItemsDT);
        }
    }
    
        
}
//在这里编写您的函数或者类
class WalmartOrderHTMLParser
{
      public string create_date_time;
      public string location;
      public string orderLink;
      public string customer_name;
      public string file;
      public WalmartOrderHTMLParser(DataRow orderExcelRow, string customerName, string 上传文件解压缩路径){
          create_date_time = orderExcelRow["Received Date"].ToString();
          location = orderExcelRow["Location"].ToString();
          orderLink = orderExcelRow["Document Link"].ToString();
          string fileName = orderExcelRow["File Name"].ToString();
          file = Path.Combine(上传文件解压缩路径, fileName);
          customer_name = customerName;
      }
    
        public void parseHtml(ref DataTable orderDT, ref DataTable orderItemsDT) 
        {
            initOrderDT(ref orderDT);
            // string file = @"C:\RPA工作目录\雀巢_沃尔玛\导出文件\HTML网页文件\2200414098-WM.html"; //@"D:\RPA 客户\雀巢\沃尔玛\order_detail2.html";
            Console.WriteLine("file: {0}", file);
            string htmlCode = File.ReadAllText(file);
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(htmlCode);
            string bodyCode = htmlDoc.DocumentNode.ChildNodes.FindFirst("html").ChildNodes.FindFirst("body").InnerHtml;
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(bodyCode);
            DataRow orderRow = orderDT.NewRow();

            // PO Number & Dates 模块
            poNumberAndDatesData(doc, ref orderRow);
            // Additional Details 模块
            additionalDetailsSectionData(doc, ref orderRow);
            // Ship To Location
            shipToSectionData(doc, ref orderRow);
            // Allowance / Charge
            orderSummaryInfo(doc, ref orderRow);
            orderRow["customer_name"] = customer_name;
            orderRow["create_date_time"] = Convert.ToDateTime(create_date_time);
            orderRow["document_link"] = orderLink;
            
            //order Items
            parseOrderItems(ref orderItemsDT, ref orderRow,  bodyCode);
            orderDT.Rows.Add(orderRow);
        }

        public void poNumberAndDatesData(HtmlDocument doc, ref DataRow orderRow)
        {
            var poNumberAndDateSection = doc.DocumentNode.SelectNodes("main//div[contains(@class, 'card-header')  and contains(text(), 'PO Number')]/following-sibling::div"); // doc.DocumentNode.SelectNodes("main//div[contains(@class, \"card-header\") and contains(text(), \"PO Number & Dates\")]/following-sibling::div");

            if (poNumberAndDateSection != null)
            {
                foreach (HtmlNode poNumAndDateDiv in poNumberAndDateSection[0].ChildNodes)
                {
                    string order_number = getNodeValue(poNumAndDateDiv, "Purchase Order Number");
                    if (!string.IsNullOrEmpty(order_number)) orderRow["order_number"] = order_number;

                    string create_date = getNodeValue(poNumAndDateDiv, "Purchase Order Date");
                    if (!string.IsNullOrEmpty(create_date)) orderRow["create_date"] = Convert.ToDateTime(create_date);

                    string shipDate = getNodeValue(poNumAndDateDiv, "Ship Not Before");
                    string shipNotLaterThan = getNodeValue(poNumAndDateDiv, "Ship No Later Than");
                    string mustArriveBy = getNodeValue(poNumAndDateDiv, "Must Arrive By");
                    if (String.IsNullOrEmpty(shipDate))
                    {
                        shipDate = getNodeValue(poNumAndDateDiv, "Ship Date");
                    }
                    if (String.IsNullOrEmpty(mustArriveBy))
                    {
                        mustArriveBy = getNodeValue(poNumAndDateDiv, "Cancel Date");
                    }
                    if (!string.IsNullOrEmpty(shipDate)) orderRow["ship_date"] = Convert.ToDateTime(shipDate);
                    if (!string.IsNullOrEmpty(mustArriveBy))  orderRow["must_arrived_by"] = Convert.ToDateTime(mustArriveBy);
                }
            }
        }

        public void additionalDetailsSectionData(HtmlDocument doc, ref DataRow orderRow)
        {
            var additionalDetailsSection = doc.DocumentNode.SelectNodes("main//div[contains(@class, 'card-header')  and contains(text(), 'Additional Details')]/following-sibling::div"); // doc.DocumentNode.SelectNodes("main//div[contains(@class, \"card-header\") and contains(text(), \"PO Number & Dates\")]/following-sibling::div");

            if (additionalDetailsSection != null)
            {
                foreach (HtmlNode poNumAndDateDiv in additionalDetailsSection[0].ChildNodes)
                {
                    string orderType = getNodeValue(poNumAndDateDiv, "Order Type");
                    if (!string.IsNullOrEmpty(orderType)) orderRow["order_type"] = orderType;
                    string promotionalEvent = getNodeValue(poNumAndDateDiv, "Promotional Event");
                    if (!string.IsNullOrEmpty(promotionalEvent)) orderRow["promotional_event"] = promotionalEvent;
                }
            }
        }
        public void shipToSectionData(HtmlDocument doc, ref DataRow orderRow)
        {
            try
            {
                var shipToSection = doc.DocumentNode.SelectNodes("main//div[contains(@class, 'card-header')  and contains(text(), 'Ship To')]/following-sibling::div"); // doc.DocumentNode.SelectNodes("main//div[contains(@class, \"card-header\") and contains(text(), \"PO Number & Dates\")]/following-sibling::div");

                if (shipToSection != null)
                {
                    HtmlNode poNumAndDateDiv = shipToSection[0].ChildNodes[1];
                    HtmlNode shipToAddrNode = poNumAndDateDiv.SelectSingleNode("label");
                    string shipToText = shipToAddrNode.InnerText;
                    string[] shipToArr = shipToText.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    orderRow["location"] = shipToArr.Length == 2 ? shipToArr[1] : "";
                }
            }
            catch
            {
                orderRow["location"] = ""; // location 来自参数
            }
        }

        public void orderSummaryInfo(HtmlDocument doc, ref DataRow orderRow)
        {
            var orderSummaryInfoTable = doc.DocumentNode.SelectNodes("main//td[contains(text(), 'Total Order Amount')]/ancestor::table"); // doc.DocumentNode.SelectNodes("main//div[contains(@class, \"card-header\") and contains(text(), \"PO Number & Dates\")]/following-sibling::div");
            var tbodyNode = orderSummaryInfoTable[0].ChildNodes.FindFirst("tbody");
            HtmlNodeCollection orderTrNodes;
            if (tbodyNode == null) {
                orderTrNodes = orderSummaryInfoTable[0].SelectNodes("tr");
            }
            else
            {
                orderTrNodes = orderSummaryInfoTable[0].SelectNodes("tbody/tr");
            }
            string allowanceOrCharge = string.Empty;
            string allowanceDescription = string.Empty;
            string allowancePercent = string.Empty;
            string allowanceTotal = string.Empty;
            string totalAmount = string.Empty;
            string totalLineItems = string.Empty;
            string totalUnitsOrdered = string.Empty;
            foreach (HtmlNode trNode in orderTrNodes)
            {
                var tdNodes = trNode.ChildNodes;
                string firstTDValue = tdNodes[1].InnerText.Trim();
                if(firstTDValue == "Allowance")
                {
                    string item2 = tdNodes[3].InnerText.Trim();
                    string item3 = tdNodes[5].InnerText.Trim();
                    string item4 = tdNodes[7].InnerText.Trim();
                    allowanceOrCharge = String.IsNullOrEmpty(allowanceOrCharge) ? firstTDValue : allowanceOrCharge + "，" + firstTDValue;
                    allowanceDescription = String.IsNullOrEmpty(allowanceDescription) ? item2 : allowanceDescription + "，" + item2;
                    allowancePercent = String.IsNullOrEmpty(allowancePercent) ? item3 : allowancePercent + "，" + item3;
                    allowanceTotal = String.IsNullOrEmpty(allowanceTotal) ? item4 : allowanceTotal + "，" + item4;
                }
                else if(firstTDValue == "Total Order Amount (After Adjustments)")
                {
                    totalAmount = tdNodes[3].InnerText.Trim();
                }
                else if(firstTDValue == "Total Line Items")
                {
                    totalLineItems = tdNodes[3].InnerText.Trim();
                }
                else if (firstTDValue == "Total Units Ordered")
                {
                    totalUnitsOrdered = tdNodes[3].InnerText.Trim();
                }
            }
            orderRow["allowance_or_charge"] = allowanceOrCharge;
            orderRow["allowance_description"] = allowanceDescription;
            orderRow["allowance_percent"] = allowancePercent;
            orderRow["allowance_total"] = allowanceTotal.Replace("(", "").Replace(")", "");
            orderRow["total_order_amount_after_adjustments"] = totalAmount;
            orderRow["total_line_items"] = totalLineItems;
            orderRow["total_units_ordered"] = totalUnitsOrdered;


            // "td[contains(text(), "Total Order Amount(After Adjustments)")]/parent::*/parent::*/parent::*"
        }
        public string getNodeValue(HtmlNode partentNode, string labelName)
        {
            HtmlNode targetNode = partentNode.SelectSingleNode(string.Format("label[contains(text(), '{0}')]/following-sibling::div", labelName));
            if (targetNode != null)
            {
                 return targetNode.InnerText.Trim();
            }
            else
            {
                return string.Empty;
            }
        }
        public void initOrderDT(ref DataTable orderDT)
        {
            List<string> orderColumns = new List<string>{"order_number", "order_type", "create_date", "create_date_time", "document_link", "ship_date",
        "must_arrived_by", "promotional_event", "location", "allowance_or_charge", "allowance_description", "allowance_percent", "allowance_total",
        "total_order_amount_after_adjustments", "total_line_items", "total_units_ordered", "customer_name", "file_path"};
            foreach (string item in orderColumns)
            {
                orderDT.Columns.Add(item, typeof(string));
            }
        }
        
        public void parseOrderItems(ref DataTable orderItemsDT, ref DataRow newOrderRow, string orderItemHtmlCode)
        {
            
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(orderItemHtmlCode);
            // td[text()="Supplier Stock #"]/parent::*
            var headers = doc.DocumentNode.SelectNodes("//tr[not(contains(@id, \"lineDetailbody\"))]/td[text()=\"Supplier Stock #\"]/parent::*/td");
            var lineItems = doc.DocumentNode.SelectNodes("//tr[contains(@id, \"lineDetailheader\")]");

            foreach (HtmlNode header in headers)
            {
                orderItemsDT.Columns.Add(header.InnerText.Trim());
            }
            int rowIndex = 1;
            foreach (HtmlNode row in lineItems)
            {
                DataRow OrderItemDRow = orderItemsDT.NewRow();
                List<string> ItemList = row.SelectNodes("td").Select(td => td.InnerText.Trim()).ToList();
                // orderItemsDT.Rows.Add(OrderItemDRow);

                if (rowIndex == 1)
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
                if (itemDescription != null)
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
                    foreach (int i in new Int32[] { 1, 2, 3, 4, 5, 6 })
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
                    foreach (int i in new Int32[] { 1 })
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

    }