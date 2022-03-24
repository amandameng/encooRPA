//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    // 清空模板数据
    Console.WriteLine("---customer_name----:{0}", customer_name);
    etoResultDT = 模板数据表.Clone();
    etoResultDT.Columns.Add("Customer Order Date");
    etoResultDT.Columns.Add("Customer Order Number");

    IEnumerable<IGrouping<string, DataRow>> groupedOrders = 新增订单连接数据表.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["order_number"].ToString());//C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值

    // 沃尔玛规则： bulkWalferConfigDT 散威化产品
    List<string> bulk_walfer_codes = new List<string>{};
    if(bulkWalferConfigDT!= null){
        foreach(DataRow dr in bulkWalferConfigDT.Rows){
            string customer_product_code = dr["customer_product_code"].ToString();
            bulk_walfer_codes.Add(customer_product_code);
        }
    }
    int 此批订单序号 = 订单序号;
    foreach(var itemGroup in groupedOrders)
    {
        DataRow[] orderItemRowsAll = itemGroup.ToArray();
        
        IEnumerable<IGrouping<string, DataRow>> groupedOrderLinks = orderItemRowsAll.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["document_link"].ToString());
        // 只需要最新的orders

        foreach(var orderLinkGroup in groupedOrderLinks){
            DataRow[] orderItemRows = orderLinkGroup.ToArray();
            
            int bulkWalferItemCount = 1;
           
            foreach(DataRow dr in orderItemRows){
                string productCode = dr["product_code"].ToString();
                int quantity_ordered = toIntConvert(dr["quantity_ordered"]);
                string 雀巢产品编码 = dr["雀巢产品编码"].ToString();
                int lineNumber = toIntConvert(dr["line_number"]);
                Console.WriteLine("雀巢产品编码: {0}", 雀巢产品编码);
                // 山姆水单，仓租不为1.3%或者产品行折扣，不录单
                if(customer_name == "山姆-IB Water" && exceptionPODT!=null && exceptionPODT.Rows.Count >0 ){
                    DataRow[] exceptionDRs = exceptionPODT.Select($"`PO No.` = '{dr["order_number"].ToString()}' and `Exception reason` like '%仓租不为1.3%订单%'");
                    if(exceptionDRs.Length > 0){
                        continue;
                    }
                    DataRow[] exceptionItemDRs = exceptionPODT.Select($"`PO No.` = '{dr["order_number"].ToString()}' and `沃尔玛产品编码` = '{productCode}' and `Exception reason` like '%产品行折扣%'");
                    if(exceptionItemDRs.Length > 0){
                        continue;
                    }
                }
                if(customer_name == "山姆"){
                    // 不填充EX2O ---
                    string remark = dr["Remark"].ToString();
                    string remarkOption = dr["Remark_Option"].ToString();
                    Console.WriteLine("remark: {0}", remark);
                    /*
                    山姆规则：
                    1.  主产品【备注】：不填充ex2o，只反馈问题订单
                        收货地址为  【 4873/4802/4817/4819 】
                        客户产品编码固定为（未来可能会新增，由SA/CSA通知）
                        01渠道：980064917，980063938，980064918，980064961，980070675，980086618；980093129；980093470；980089840
                        02渠道：980066573，980056574，980061557
                        不录单，直接反馈Exception
                        Exception reason：未录单，水仓+干货产品
                    
                    2.  主产品【特殊备注检查项】：订单类型Promotional Event为JD，且含以下雀巢码的反馈Exception
                       山姆京东类型的订单如下单产品为氨糖奶粉，不录单作为exception order发给CSA取消
                       Exception reason：不录单，山姆JD+氨糖奶粉
                       山姆订单9050576478，12461142 氨糖要录单，山姆京东类型的订单+氨糖才不录
                    */
                    if(remark.Contains("不填充ex2o") || (remarkOption.Contains("氨糖奶粉反馈Exception") && dr["promotional_event"].ToString().Contains("JD"))){
                        Console.WriteLine("-----------------不填充ex2o || 山姆JD+氨糖奶粉----------");
                        continue;
                    }
                }
                if(bulk_walfer_codes.Contains(productCode)){
                    /*
                    沃尔玛规则：
                      如果是散威化商品
                      当订单数量＜10时，全部录入巧克力口味
                      数量≥10，口味按巧克力：牛奶：花生：  7：2：1进行拆分
                    
                    山姆 1：1：1产品
                    */
                    if(customer_name == "沃尔玛"){
                        walmartBulkWalfer(productCode, quantity_ordered, 雀巢产品编码, ref bulkWalferItemCount, dr);
                    }else if(customer_name == "山姆"){
                        samOneToManyProcess(productCode, quantity_ordered, 雀巢产品编码, ref bulkWalferItemCount, dr);
                    }
                }else{
                    

                    
                    /*
                    山姆水单：订单抓单 Promotional Event订单类型中带有JD字样类型的订单都当作异常订单反馈CSA
                    Exception reason：JD订单不处理，不录单
                    update on 2021-12-15： 【山姆水JD订单，需要正常采集，填写excel to orde】
                    if(customer_name == "山姆-IB Water" && dr["promotional_event"].ToString().Contains("JD")){
                        continue;
                    }
                    */
                    
                    DataRow etoRow = etoResultDT.NewRow();
                    // 山姆与雀巢产品数量对照
                    /*
                    由于部分产品山姆和雀巢的规格不一致，需要按照比例进行转换，会存在雀巢数量不为整数的情况：
                    1, 山姆产品980070675（雀巢营养谷物迷你装）对应雀巢产品数量不为整数时，按照以下例子计算逻辑计算并录单。录单完毕后再作为exception反馈出来备注原订单数量是多少，录单后数量是多少
                    
                    例:
                    当山姆原单数量为2507时（山姆一箱对应雀巢0.6箱），雀巢数量为 2507*0.6=1504.2，不为整数，因此需要自主换算山姆箱数，
                    固定规则: 按照山姆整层30箱为标准送货
                    --2507/30=83.56（向下取整数83）
                    --83*30=2490箱
                      即按照山姆数量2490箱出单
                    
                    2, 其余产品换算雀巢数量不为整数的情况不录单反馈Exception
                    */
                    if(samQtyMappingDT!=null){
                        DataRow[] qtyMappingRows = samQtyMappingDT.Select(string.Format("Sam_Product_Code='{0}' and Nestle_Product_Code = '{1}'", productCode, 雀巢产品编码));
                        if(qtyMappingRows.Length > 0){
                            DataRow qtyMappingRow = qtyMappingRows[0];
                            bool 是否录单 = false;
                            quantity_ordered = toIntConvert(fetchQty(quantity_ordered, qtyMappingRow, ref 是否录单));
                            if(!是否录单){
                                Console.WriteLine("-----------------箱数不为整数不录单----------");
                                continue; // 继续下一条，这一条不进ETO
                            }
                            Decimal cost = Convert.ToDecimal(dr["cost"]);
                            // Cost_Check_Value 设置了值
                            if(!string.IsNullOrEmpty(qtyMappingRow["Cost_Check_Value"].ToString())){
                                Decimal costCheckValue = Convert.ToDecimal(qtyMappingRow["Cost_Check_Value"]);
                                if(costCheckValue != cost){ // 客户网站cost跟设定的不match，则不录单
                                  Console.WriteLine("-----------------客户网站cost跟设定的不match，则不录单----------");
                                  continue;
                                }
                            }
                        }
                    }

                    initEtoRow(ref etoRow, dr, quantity_ordered, 雀巢产品编码, bulkWalferItemCount, 此批订单序号);
                    bulkWalferItemCount += 1;
                    etoResultDT.Rows.Add(etoRow);
                }
            }

           break;
        }
        此批订单序号 += 1;
    }
    
    etoResultToExcelDT = etoResultDT.Copy();
    etoResultToExcelDT.Columns.Remove("Customer Order Date");
    etoResultToExcelDT.Columns.Remove("Customer Order Number");

   // Convert.ToInt16("aa");
}

public decimal fetchQty(object originalQty, DataRow qtyMappingRow, ref bool 是否录单){
    decimal customerOrderQty = toDecimalConvert(originalQty);
    
    string Not_Integer_Still_Into_EX2O = qtyMappingRow["Not_Integer_Still_Into_EX2O"].ToString();
    decimal nestleQty_m = customerOrderQty * toDecimalConvert(qtyMappingRow["Nestle_Qty"]);
    int nestleQtyInt = toIntConvert(nestleQty_m);
    // 换算不为整数则，看产品设定是否录单,反馈exception。
    // 1、如果计算后箱数不为整数，但是【Not_Integer_Still_Into_EX2O】为1，则计算出整数录单，否则不录单。
    // 2、如果计算后箱数为整数，则都录单
    if((nestleQtyInt != nestleQty_m)){
        if(Not_Integer_Still_Into_EX2O == "1"){
            int 山姆整层箱数 = 30;
            int 层数 = toIntConvert(customerOrderQty/山姆整层箱数);
            decimal quantity_ordered = 层数 * 山姆整层箱数;
            是否录单 = true;
            return quantity_ordered;
        }else{
            return nestleQty_m;
        }
    }else{
        是否录单 = true;
        return nestleQty_m;
    }
}

/*
沃尔玛规则：
  如果是散威化商品
  当订单数量＜10时，全部录入巧克力口味
  数量≥10，口味按巧克力：牛奶：花生：  7：2：1进行拆分
*/
public void walmartBulkWalfer(string productCode, int quantity_ordered, string 雀巢产品编码, ref int bulkWalferItemCount, DataRow dr){
    DataRow bulkWalferProduct = bulkWalferConfigDT.Select("customer_product_code='" + productCode + "'")[0];
    int boundry_value = toIntConvert(bulkWalferProduct["boundry_value"]);
    if(quantity_ordered < boundry_value){
        雀巢产品编码 = bulkWalferProduct["nestle_code_default"].ToString();
        DataRow etoRow = etoResultDT.NewRow();
        initEtoRow(ref etoRow, dr, quantity_ordered, 雀巢产品编码, bulkWalferItemCount, 0);
        etoResultDT.Rows.Add(etoRow);
    }else{
        /* 
        customer_product_code  boundry_value  nestle_code_default  nestle_code_allocation  allocation_ratio  flavor_description  bulk_walfer_type
        021402419  10  12458252  12458252,12458087,12458320  7：2：1  巧克力：牛奶：花生  散威化
        021779181  10  12458252  12458252,12458087,12458320  7：2：1  巧克力：牛奶：花生  散威化
        */
        string nestleCodeAllocation = bulkWalferProduct["nestle_code_allocation"].ToString();
        string allocationRatio = bulkWalferProduct["allocation_ratio"].ToString();
        string[] nestleCodeArr = nestleCodeAllocation.Split(new string[]{",", "，"}, StringSplitOptions.RemoveEmptyEntries);
        string[] allocationRatioArr = allocationRatio.Split(new string[]{"：", ":"}, StringSplitOptions.RemoveEmptyEntries); // 注意： 是中文冒号
        int[] qtyArr = splitQtyByRatio(allocationRatioArr, quantity_ordered);

        for(int i=0; i< nestleCodeArr.Length; i++){
            string nestleBulkWalferCode = nestleCodeArr[i];
            int curQuantity_ordered = qtyArr[i];
            // Console.WriteLine("curQuantity_ordered: {0}", curQuantity_ordered);
            DataRow etoRow = etoResultDT.NewRow();
            initEtoRow(ref etoRow, dr, curQuantity_ordered, nestleBulkWalferCode, bulkWalferItemCount, 0);
            bulkWalferItemCount = bulkWalferItemCount + 1;
            etoResultDT.Rows.Add(etoRow);
        }
    }
}

/*
山姆1：N产品，比例是 1：1：1, 假设山姆的产品Qty为180，那么需要衍生出3条产品行数据，每一行的Qty都是180.
*/
public void samOneToManyProcess(string productCode, int quantity_ordered, string 雀巢产品编码, ref int bulkWalferItemCount, DataRow dr){
    DataRow bulkWalferProduct = bulkWalferConfigDT.Select("customer_product_code='" + productCode + "'")[0];
    string nestleCodeAllocation = bulkWalferProduct["nestle_code_allocation"].ToString();
    string allocationRatio = bulkWalferProduct["allocation_ratio"].ToString();
    string[] nestleCodeArr = nestleCodeAllocation.Split(new string[]{",", "，"}, StringSplitOptions.RemoveEmptyEntries);
    string[] allocationRatioArr = allocationRatio.Split(new string[]{"：", ":"}, StringSplitOptions.RemoveEmptyEntries); // 注意： 是中文冒号
    int[] qtyArr = reAllocateQty(allocationRatioArr, quantity_ordered);
    for(int i=0; i< nestleCodeArr.Length; i++){
        string nestleCode = nestleCodeArr[i];
        int curQuantity_ordered = qtyArr[i];

        DataRow etoRow = etoResultDT.NewRow();
        initEtoRow(ref etoRow, dr, curQuantity_ordered, nestleCode, bulkWalferItemCount, 0);
        bulkWalferItemCount = bulkWalferItemCount + 1;
        etoResultDT.Rows.Add(etoRow);
    }
}

// 按比列重分配，山姆谷物
public int[] reAllocateQty(string[] allocationRatioArr, int quantity_ordered){
    List<int> initQtyList = new List<int> {};
    foreach(string ratioStr in allocationRatioArr){
        int rationValue = toIntConvert(ratioStr);
        int curQuantity_ordered = quantity_ordered * rationValue;
        initQtyList.Add(curQuantity_ordered);
    }
    return initQtyList.ToArray();
}


// 按整体1比列拆分，沃尔玛散威化
public int[] splitQtyByRatio(string[] allocationRatioArr, int quantity_ordered){
    decimal total_ratio = 0.0m;
    foreach(string ratio in allocationRatioArr){
        total_ratio = total_ratio + Convert.ToDecimal(ratio);
    }
        
    List<int> initQtyList = new List<int> {};
    int totalRequltQty = 0;
    foreach(string ratioStr in allocationRatioArr){
        int rationValue = toIntConvert(ratioStr);
        int curQuantity_ordered = toIntConvert(Math.Round(quantity_ordered * (rationValue/total_ratio)));
        totalRequltQty += curQuantity_ordered;
        initQtyList.Add(curQuantity_ordered);
    }
    if(quantity_ordered != totalRequltQty){
        int firstQty = initQtyList[0];
        initQtyList[0] = firstQty + (quantity_ordered - totalRequltQty);
    }
    return initQtyList.ToArray();
}

//订单序号 只有沃尔玛ICE CREAM 用到
public void initEtoRow(ref DataRow etoRow, DataRow dr, int quantity_ordered, string 雀巢产品编码, int lineNumber, int 订单序号){
   //  DataRow etoRow = etoResultDT.NewRow();
   
    etoRow["Order Type"] = "OR";
    etoRow["Sales Org"] = ((customer_name == "沃尔玛IC") ? "CN23" : "CN26");
    etoRow["Distribution channel"] = string.IsNullOrEmpty(dr["Distribution_Channel"].ToString()) ? "01" : dr["Distribution_Channel"].ToString();
    etoRow["Sold to"] = dr["sold_to_code"].ToString();
    etoRow["Ship to"] = dr["ship_to_code"].ToString();
    
    etoRow[7] = dr["line_number"].ToString(); // PO: 2 位于excel第8列
    
    etoRow["PO Number"] = getPONumber(dr, 订单序号);
    etoRow["Reqd Del Date"] = DateTime.Parse(dr["request_delivery_date"].ToString()).ToString("yyyyMMdd");
    etoRow["SAP Material"] = 雀巢产品编码;
    
    etoRow["Qty"] = quantity_ordered;
    etoRow["UoM"] = "CS";
    etoRow["PO"] = lineNumber;
    etoRow["Customer Order Date"] = dr["create_date"]; // !!! not in excel template
    etoRow["Customer Order Number"] = dr["order_number"]; // !!! not in excel template

    // 沃尔玛ICE CREAM
    if(customer_name == "沃尔玛IC"){
        etoRow["Delivery note text"] = $"即走，起送日期{DateTime.Parse(dr["ship_date"].ToString()).ToString("yyyyMMdd")}; 最后送货日期{etoRow["Reqd Del Date"].ToString()}";
    }
}

public string getPONumber(DataRow dr, int 订单序号){
    string orderNumber = dr["order_number"].ToString();
    if(customer_name == "山姆-IB Water"){
        return $"IBU{orderNumber}";
    }else if(customer_name == "沃尔玛IC"){
        string mmdd = DateTime.Now.ToString("MMdd");
        string WMDC = dr["WMDC"].ToString();
        // 4001014040-DGPDC-1021-1
        return $"{orderNumber}-{WMDC}-{mmdd}-{订单序号}";
    }else{
       return orderNumber;
    }
}

// 转decimal
public static decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
        Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

// 转int
public static int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
        Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}