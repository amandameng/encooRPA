public static string SaveExcelFolderPathStr = @"C:\RPA\Nestle\leyou\Outputs\";

public void Run()
        {
            
            #region Read Order Files

            OrderReader reader = new OrderReader();
            //string filePath = @"D:\Documents\Nestle\leyou\Orders";
            DirectoryInfo theFolder = new DirectoryInfo(ordersFilePath);

            System.Data.DataTable orderTable = TableInit.GetOrderTable();

            foreach (FileInfo file in theFolder.GetFiles())
            {
                try
                {
                    reader.LoadHtmlFromFileFullName(file.FullName);
                    orderTable.Rows.Add(reader.GetOrderRow().ItemArray);
                }
                catch
                {
                    System.Console.WriteLine("Read File Error, Check Files " + file.FullName);
                    MailUtility.SendErrorMail(sendMailObj, DataUtility.GetAlertEmailAddress(), Issues.GetAlertMessage(file.Name));
                    continue;
                }
            }
            #endregion

            #region DataCheck

            DataCheck dataCheck = new DataCheck(orderTable);
            //Check duplicate orders
            HashSet<string> duplicateList = dataCheck.CheckDuplicatePO(DataUtility.GetExistPO());
            Console.WriteLine($"duplicateList Count: {duplicateList.Count}");
            //Check Order Data
            dataCheck.CheckAll();

            #endregion

            #region Generate and Save Excel Table
            GenerateExcelTable gen = new GenerateExcelTable(orderTable);
            System.Data.DataTable cleanOrder = gen.GenCleanOrderTable();
            System.Data.DataTable exceptionOrder = gen.GenExceptionTable();
            System.Data.DataTable exceltoOrder = gen.GenExcelToTable();

            string cleanFile = SaveExcelFile.SaveDataTable2Excel(cleanOrder, "CleanOrder");
            string exceptionFile = SaveExcelFile.SaveDataTable2Excel(exceptionOrder, "ExceptionOrder");
            string exceltoFile = SaveExcelFile.SaveDataTable2Excel(exceltoOrder, "ExceltoOrder");

            #endregion
            
            #region Update DataBase

            DataUtility.UpdateDataBase(TableInit.GetOrderTable4DB(orderTable), "leyou_orders");
            System.Data.DataTable itemTable = TableInit.GetItemTable();
            foreach (DataRow orderRow in orderTable.Rows)
            {
                itemTable.Merge((System.Data.DataTable)orderRow["ItemTable"]);
            }
            DataUtility.UpdateDataBase(TableInit.GetItemTable4DB(itemTable), "leyou_orders_items");
            DataUtility.UpdateDataBase(TableInit.GetCleanTable4DB(cleanOrder), "clean_order");
            DataUtility.UpdateDataBase(TableInit.GetExceptionTable4DB(exceptionOrder), "exception_order");
            DataUtility.UpdateDataBase(TableInit.GetExceltoTable4DB(exceltoOrder), "excel_to_order");

            #endregion

            #region Send Email

            MailUtility.SendResultMail(sendMailObj, DataUtility.GetEmailAddress("Clean Order", email4check.UserName), cleanFile, cleanOrder, duplicateList);
            MailUtility.SendResultMail(sendMailObj, DataUtility.GetEmailAddress("Exception Order", email4check.UserName), exceptionFile, exceptionOrder, duplicateList);
            MailUtility.SendResultMail(sendMailObj, DataUtility.GetEmailAddress("Excel To Order", email4check.UserName), exceltoFile, exceltoOrder, duplicateList);

            #endregion

        }

internal class DataCheck
    {
        public System.Data.DataTable OrderTable { get; }

        public static DataUtility.SetIssuesByOrderRow setIssuesByOrderRow = new DataUtility.SetIssuesByOrderRow(SetOrderIssues);
        public static DataUtility.SetIssuesByOrderId setIssuesByOrderId = new DataUtility.SetIssuesByOrderId(SetOrderIssues);
        public static DataUtility.SetIssuesByItemRow setIssuesByItemRow = new DataUtility.SetIssuesByItemRow(SetItemIssues);
        //public static DataUtility.SetIssuesByItemId setIssuesByItemId = new DataUtility.SetIssuesByItemId(SetItemIssues);
        public DataCheck(System.Data.DataTable orderTable)
        {
            OrderTable = orderTable;
        }

        private static void SetOrderIssues(string orderId, System.Data.DataTable orderTable, Issues.IssuesType issues)
        {
            DataRow orderRow = orderTable.Select("PO单号 = '" + orderId + "'")[0];
            orderRow["Issues"] = orderRow["Issues"].ToString() + ((int)issues).ToString();
        }

        private static void SetOrderIssues(DataRow orderRow, Issues.IssuesType issues)
        {
            orderRow["Issues"] = orderRow["Issues"].ToString() + ((int)issues).ToString();
        }

        private static void SetItemIssues(DataRow itemRow, Issues.IssuesType issues)
        {
            itemRow["Issues"] = itemRow["Issues"].ToString() + ((int)issues).ToString();
        }

        private void CalcNums()
        {
            foreach (DataRow orderRow in OrderTable.Rows)
            {
                int num = 0;
                foreach (DataRow itemRow in ((System.Data.DataTable)orderRow["ItemTable"]).Rows)
                {
                    DataRow materialRow = DataUtility.GetMaterialRow(itemRow, SetItemIssues);
                    if (materialRow != null)
                    {
                        num += Convert.ToInt32(itemRow["数量"]) / Convert.ToInt32(materialRow["Nestle_Case_Configuration"]);
                    }                    
                }
                orderRow["NumsByCS"] = num;
            }
        }

        private void CalcPriceDiff()
        {
            decimal nestlePrice, leyouPrice, itemPriceDiff;

            foreach (DataRow orderRow in OrderTable.Rows)
            {
                decimal orderPriceDiff = 0;
                foreach (DataRow itemRow in ((System.Data.DataTable)orderRow["ItemTable"]).Rows)
                {

                    DataRow materialRow = DataUtility.GetMaterialRow(itemRow, SetItemIssues);

                    if (materialRow != null)
                    {
                        Decimal.TryParse(materialRow["Nestle_NPS"].ToString(), out decimal nestleUnitPrice);
                        int.TryParse(materialRow["Nestle_Case_Configuration"].ToString(), out int itemSpec);

                        if (Decimal.TryParse(itemRow["单价"].ToString(), out decimal leyouUnitPrice) &&
                            int.TryParse(itemRow["数量"].ToString(), out int itemNums))
                        {
                            nestlePrice = nestleUnitPrice / itemSpec * itemNums * (1 - DataUtility.GetDeductionPoint());
                            leyouPrice = leyouUnitPrice / 1.13M * (1 - DataUtility.GetDeductionPoint()) * itemNums;
                            itemPriceDiff = nestlePrice - leyouPrice;
                            itemRow["PriceDiff"] = itemPriceDiff;
                        }
                        else
                        {
                            throw new Exception("Failed to Parse nums fetched from file.");
                        }
                    }
                    else
                    {
                        continue;
                    }

                    orderPriceDiff += itemPriceDiff;

                }
                orderRow["PriceDiff"] = orderPriceDiff;
            }
        }

        private bool CheckeOrderPriceDiff(DataRow orderRow)
        {
            decimal priceDiff = Convert.ToDecimal(orderRow["PriceDiff"]);
            if (priceDiff >= 20 || priceDiff <= -20)
            {
                SetOrderIssues(orderRow, Issues.IssuesType.订单价格差异);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void CheckOrderNumsIsInt(DataRow itemRow, DataRow orderRow)
        {

            DataRow materialRow = DataUtility.GetMaterialRow(itemRow, SetItemIssues);
            if (materialRow != null)
            {
                int.TryParse(itemRow["数量"].ToString(), out int itemNums);
                int.TryParse(materialRow["Nestle_Case_Configuration"].ToString(), out int spec);
                if (itemNums % spec != 0)
                {
                    SetItemIssues(itemRow, Issues.IssuesType.非整数订单);
                }
            }
        }

        private void CheckNegativeNumOrder(DataRow orderRow)
        {
            if ((decimal)orderRow["总额"] <= 0)
            {
                SetOrderIssues(orderRow, Issues.IssuesType.负数价格);
            }
        }

        private void CheckAddress(DataRow orderRow)
        {
            string address = orderRow["供应商送货地址"].ToString();
            string remark = orderRow["备注"].ToString();
            if (!remark.Contains(address))
            {
                SetOrderIssues(orderRow, Issues.IssuesType.送货地址有误订单);
            }
        }

        private void CheckIsValiDateCross(DataRow orderRow)
        {
            DateTime startDate = (DateTime)orderRow["有效时间_start"];
            DateTime endDate = (DateTime)orderRow["有效时间_end"];

            if (startDate.Month != endDate.Month)
            {
                SetOrderIssues(orderRow, Issues.IssuesType.跨月订单);
            }
        }

        private void CheckValiDate(DataRow orderRow)
        {
            DateTime startDate = (DateTime)orderRow["有效时间_start"];
            DateTime endDate = (DateTime)orderRow["有效时间_end"];
            DateTime readDate = (DateTime)orderRow["Created_time"];

            if ((endDate - readDate).Days < 4)
            {
                SetOrderIssues(orderRow, Issues.IssuesType.送货日问题订单);
            }
        }

        private void CheckIsSpecial(DataRow itemRow)
        {
            var materialRow = DataUtility.GetMaterialRow(itemRow, SetItemIssues);
            string remark = String.Empty;
            if (materialRow != null)
            {
                remark = materialRow["Remark"].ToString();
            }

            if (remark.Equals("特殊产品"))
            {
                SetItemIssues(itemRow, Issues.IssuesType.特殊产品订单);
            }
        }

        public void CheckAll()
        {
            CalcNums();
            CalcPriceDiff();

            foreach (DataRow orderRow in OrderTable.Rows)
            {
                bool isPriceOK = CheckeOrderPriceDiff(orderRow);
                CheckNegativeNumOrder(orderRow);
                CheckAddress(orderRow);
                CheckIsValiDateCross(orderRow);
                CheckValiDate(orderRow);

                foreach (DataRow itemRow in ((System.Data.DataTable)orderRow["ItemTable"]).Rows)
                {
                    CheckOrderNumsIsInt(itemRow, orderRow);
                    CheckIsSpecial(itemRow);
                }
            }
        }
        
        public HashSet<string> CheckDuplicatePO(HashSet<string> list)
        {
            HashSet<string> result = new HashSet<string>();
            string poN;

            for (int i = OrderTable.Rows.Count - 1; i >= 0; i--)
            {
                poN = OrderTable.Rows[i]["PO单号"].ToString();
                if (list.Contains(poN))
                {
                    result.Add(poN);
                    OrderTable.Rows.RemoveAt(i);
                }                
            }
            return result;
        }
        
    }


internal static class DataUtility
    {
        private const string connStr = @"Server=1.15.243.91;Database=vicode_nestle; Uid=nestle;Password=encoo@123456;Convert Zero Datetime=True";
        private const string leyouQueryStr = @"SELECT * FROM `leyou_orders`;
                                 SELECT * FROM `leyou_orders_items`;
                                 SELECT * FROM `material_master_data` WHERE Customer_Name = '乐友';
                                 SELECT * FROM `sold_to_ship_to` WHERE Customer_Name = '乐友'";
        private const string po_QueryStr = @"SELECT `PO单号` FROM `leyou_orders`";
        private const string leyouMaterialQueryStr = @"SELECT * FROM `material_master_data` WHERE Customer_Name = '乐友'";
        private const string leyouSoldtoshiptoQueryStr = @"SELECT * FROM `sold_to_ship_to` WHERE Customer_Name = '乐友'";
        private const string leyouEmailQueryStr = @"SELECT Order_Category, Mail_Receipt_Address FROM `mail_setting` WHERE Customer_Name = '乐友'";
        private const string leyouAlertEmailQueryStr = @"SELECT `Flow Alert Receiver Email Address` FROM `rpa_accounts` WHERE `Customer Name` = '乐友'";
        private const string leyouDeductionPointQueryStr = @"SELECT `discount_rate` FROM `excel_to_order_config` WHERE `customer` = '乐友'";
        private static readonly System.Data.DataTable MaterialTable = GetTable(leyouMaterialQueryStr);

        public static DataSet GetDataSet()
        {
            DataSet ds = new DataSet();
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = leyouQueryStr;
                conn.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(ds);
                conn.Close();
            }
            return ds;
        }
        
        public static HashSet<string> GetExistPO()
        {
            HashSet<string> list = new HashSet<string>();
            var dt = GetTable(po_QueryStr);
            foreach (DataRow dr in dt.Rows)
            {
                list.Add(dr[0].ToString());
            }
            return list;
        }

        public static System.Data.DataTable GetTable(string queryStr)
        {
            System.Data.DataTable table = new System.Data.DataTable();
            using (MySqlConnection connection = new MySqlConnection(connStr))
            {
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = queryStr;
                connection.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(table);
                connection.Close();
            }
            return table;
        }
        public delegate void SetIssuesByOrderRow(DataRow orderRow, Issues.IssuesType issues);
        public delegate void SetIssuesByOrderId(string orderId, System.Data.DataTable orderTable, Issues.IssuesType issues);
        public delegate void SetIssuesByItemRow(DataRow itemRow, Issues.IssuesType issues);
        
        public static decimal GetDeductionPoint()
        {
            var value = GetTable(leyouDeductionPointQueryStr).Rows[0][0];
            if (!(value is DBNull) || value != null)
            {
                decimal num = decimal.Parse(value.ToString().TrimEnd(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.PercentSymbol.ToCharArray())) / 100m;
                return num;
            }
            else
            {
                return 0;
            }
        }

        public static string GetEmailAddress(string order_category)
        {
            return GetTable(leyouEmailQueryStr).Select("Order_Category = '" + order_category + "'")[0]["Mail_Receipt_Address"].ToString();
        }
        
        public static string GetEmailAddress(string order_category, string email4checkUserName)
        {
            return GetEmailAddress(order_category) + "/" + email4checkUserName;
        }
        
        public static string GetAlertEmailAddress()
        {
            return GetTable(leyouAlertEmailQueryStr).Rows[0][0].ToString();
        }

        public static DataRow GetSoldtoshiptoRow(DataRow orderRow, SetIssuesByOrderRow setIssues)
        {
            DataRow[] rowArr = DataUtility.GetTable(DataUtility.leyouSoldtoshiptoQueryStr).Select("Delivery_Address LIKE '%" + orderRow["供应商送货地址"].ToString() + "%'");
            if (rowArr.Length > 0)
            {
                return rowArr[0];
            }
            else
            {
                return null;
            }
        }

        public static DataRow GetMaterialRow(DataRow itemRow)
        {
            DataRow[] materialRowArr = MaterialTable.Select("Customer_Material_No = '" + itemRow["乐友货号"].ToString() + "'");
            if (materialRowArr != null && materialRowArr.Length > 0)
            {
                return materialRowArr[0];
            }
            else
            {
                return null;
            }
        }

        public static DataRow GetMaterialRow(DataRow itemRow, SetIssuesByItemRow setIssues)
        {
            DataRow[] materialRowArr = MaterialTable.Select("Customer_Material_No = '" + itemRow["乐友货号"].ToString() + "'");
            if (materialRowArr != null && materialRowArr.Length > 0)
            {
                return materialRowArr[0];
            }
            else
            {
                setIssues(itemRow, Issues.IssuesType.无法mapping雀巢产品);
                return null;
            }
        }

        //public static DataRow GetMaterialRow(DataRow itemRow, System.Data.DataTable orderTable, SetIssuesByOrderId setIssues)
        //{
        //    DataRow[] materialRowArr = MaterialTable.Select("Customer_Material_No = '" + itemRow["乐友货号"].ToString() + "'");
        //    if (materialRowArr != null && materialRowArr.Length > 0)
        //    {
        //        return materialRowArr[0];
        //    }
        //    else
        //    {
        //        setIssues(itemRow["order_id"].ToString(), orderTable, Issues.IssuesType.无法mapping雀巢产品);
        //        return null;
        //    }
        //}

        public static void UpdateDataBase(System.Data.DataTable dataTable, string tableName)
        {
            using (MySqlConnection connection = new MySqlConnection(connStr))
            {
                connection.Open();

                using (MySqlTransaction tran = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = tran;
                        cmd.CommandText = $"SELECT * FROM " + tableName + " limit 0";

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.UpdateBatchSize = 10000;
                            using (MySqlCommandBuilder cb = new MySqlCommandBuilder(adapter))
                            {
                                cb.SetAllValues = true;
                                adapter.Update(dataTable);
                                tran.Commit();
                            }
                        };
                    }
                }
                connection.Close();
            }
        }
    }

internal class GenerateExcelTable
    {
        private Dictionary<string, string> m_DCDic = new Dictionary<string, string>()
        {
            {"乐友沈阳","SY" },
            {"乐友天津","TJ" },
            {"乐友西安","XA" },
            {"乐友成都","CD" },
            {"乐友上海","SH" },
            {"乐友武汉","WH" },
            {"乐友青岛","QD" }
        };
        private System.Data.DataTable OrderTable { get; }

        public GenerateExcelTable(System.Data.DataTable orderTable)
        {
            OrderTable = orderTable;
        }

        private string GetNestleMaterialNo(DataRow itemRow)
        {
            var materialRow = DataUtility.GetMaterialRow(itemRow);
            if (materialRow != null)
            {
                return materialRow["Nestle_Material_No"].ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        private string GetNestlePONo(DataRow orderRow)
        {
            StringBuilder result = new StringBuilder();
            DataRow soldtoshiptoRow = DataUtility.GetSoldtoshiptoRow(orderRow, DataCheck.setIssuesByOrderRow);
            return soldtoshiptoRow == null ? string.Empty : result.Append("BSYLY").Append(m_DCDic[soldtoshiptoRow["Region"].ToString()]).Append(orderRow["PO单号"]).ToString();
        }

        private string GetPlant(DataRow orderRow)
        {
            DataRow soldtoshiptoRow = DataUtility.GetSoldtoshiptoRow(orderRow, DataCheck.setIssuesByOrderRow);
            return soldtoshiptoRow == null ? string.Empty : soldtoshiptoRow["Nestle_Plant_No"].ToString();
        }

        private string GetSoldto(DataRow orderRow)
        {
            DataRow soldtoshiptoRow = DataUtility.GetSoldtoshiptoRow(orderRow, DataCheck.setIssuesByOrderRow);
            return soldtoshiptoRow == null ? string.Empty : soldtoshiptoRow["Sold_to_Code"].ToString();
        }

        private string GetShipto(DataRow orderRow)
        {
            DataRow soldtoshiptoRow = DataUtility.GetSoldtoshiptoRow(orderRow, DataCheck.setIssuesByOrderRow);
            return soldtoshiptoRow == null ? string.Empty : soldtoshiptoRow["Ship_to_Code"].ToString();
        }

        private string GetBU(DataRow itemRow)
        {
            var materialRow = DataUtility.GetMaterialRow(itemRow);
            if (materialRow != null)
            {
                return materialRow["Nestle_BU"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        private decimal GetNestle_NPS(DataRow itemRow)
        {
            var materialRow = DataUtility.GetMaterialRow(itemRow);
            if (materialRow != null)
            {
                decimal.TryParse(materialRow["Nestle_NPS"].ToString(), out decimal result);
                return result;
            }
            else
            {
                return 0;
            }
        }

        private int GetSpec(DataRow itemRow)
        {
            var materialRow = DataUtility.GetMaterialRow(itemRow);
            if (materialRow != null)
            {
                return Convert.ToInt32(materialRow["Nestle_Case_Configuration"]);
            }
            else
            {
                return 0;
            }
        }

        private string GetPlantNo(DataRow orderRow)
        {
            var soldtoshiptoRow = DataUtility.GetSoldtoshiptoRow(orderRow, DataCheck.setIssuesByOrderRow);
            if (soldtoshiptoRow != null)
            {
                return soldtoshiptoRow["Nestle_Plant_No"].ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        private Issues.IssuesType IntConvertToEnum(int i)
        {
            return (Issues.IssuesType)Enum.ToObject(typeof(Issues.IssuesType), i);
        }

        private string GetIssues (DataRow itemRow, DataRow orderRow)
        {
            StringBuilder result = new StringBuilder();
            string orderIssuesArr = orderRow["Issues"].ToString().Trim();
            string itemIssuesArr = itemRow["Issues"].ToString().Trim();
            foreach (char c in orderIssuesArr)
            {
                int index = orderIssuesArr.IndexOf(c);
                if (index == orderIssuesArr.Length - 1 && itemIssuesArr.Length == 0)
                {
                    result.Append(IntConvertToEnum(Convert.ToInt32(c) - 48).ToString());
                }
                else
                {
                    result.AppendLine(IntConvertToEnum(Convert.ToInt32(c) - 48).ToString());
                }                
            }

            foreach (char c in itemIssuesArr)
            {
                int index = itemIssuesArr.IndexOf(c);
                if (index == itemIssuesArr.Length - 1)
                {
                    result.Append(IntConvertToEnum(Convert.ToInt32(c) - 48).ToString());
                }
                else
                {
                    result.AppendLine(IntConvertToEnum(Convert.ToInt32(c) - 48).ToString());
                }
            }

            return result.ToString();
        }

        private string GetIssueDetail (DataRow itemRow, DataRow orderRow)
        {
            StringBuilder result = new StringBuilder();
            string orderIssuesArr = orderRow["Issues"].ToString().Trim();
            string itemIssuesArr = itemRow["Issues"].ToString().Trim();
            foreach (char c in orderIssuesArr)
            {
                int index = orderIssuesArr.IndexOf(c);
                if (index == orderIssuesArr.Length - 1 && itemIssuesArr.Length == 0)
                {
                    result.Append(Issues.issuesDisc[IntConvertToEnum(Convert.ToInt32(c) - 48)]);
                }
                else
                {
                    result.AppendLine(Issues.issuesDisc[IntConvertToEnum(Convert.ToInt32(c) - 48)]);
                }
            }

            foreach (char c in itemIssuesArr)
            {
                int index = itemIssuesArr.IndexOf(c);
                if (index == itemIssuesArr.Length - 1)
                {
                    result.Append(Issues.issuesDisc[IntConvertToEnum(Convert.ToInt32(c) - 48)]);
                }
                else
                {
                    result.AppendLine(Issues.issuesDisc[IntConvertToEnum(Convert.ToInt32(c) - 48)]);
                }
            }

            return result.ToString();
        }

        private decimal CalcPriceDiffPerCS(DataRow itemRow)
        {
            int spec = GetSpec(itemRow);
            int num = Convert.ToInt32(itemRow["数量"]);
            decimal priceDiff = 0;
            if (itemRow != null && !(itemRow["PriceDiff"] is DBNull))
            {
                priceDiff = Convert.ToDecimal(itemRow["PriceDiff"]);
            }
            return priceDiff / num * spec;
        }


        public System.Data.DataTable GenCleanOrderTable()
        {
            System.Data.DataTable cleanTable = TableInit.GetCleanTable();
            foreach (DataRow orderRow in OrderTable.Rows)
            {
                if (orderRow["Issues"].ToString() != string.Empty)
                {
                    continue;
                }

                DataRow cleanRow = cleanTable.NewRow();

                cleanRow["渠道"] = "01";
                cleanRow["读单当天日期"] = ((DateTime)orderRow["Created_time"]).ToString("MM/dd/yyyy");
                cleanRow["客户名称"] = "乐友";
                cleanRow["雀巢PO No"] = GetNestlePONo(orderRow);
                cleanRow["客户系统PO No"] = orderRow["PO单号"];
                cleanRow["订单数量(单位CS)"] = orderRow["NumsByCS"];
                cleanRow["Plant/区域"] = GetPlant(orderRow);
                cleanRow["订单有效期截至日期"] = ((DateTime)orderRow["有效时间_end"]).ToString("MM/dd/yyyy");
                cleanRow["备注"] = orderRow["备注"];

                cleanTable.Rows.Add(cleanRow);
            }

            return cleanTable;
        }

        public System.Data.DataTable GenExceptionTable()
        {
            System.Data.DataTable exceptionTable = TableInit.GetExceptionTable();
            foreach (DataRow orderRow in OrderTable.Rows)
            {
                foreach (DataRow itemRow in ((System.Data.DataTable)orderRow["ItemTable"]).Rows)
                {
                    if (orderRow["Issues"].ToString() == string.Empty && itemRow["Issues"].ToString() == String.Empty)
                    {
                        continue;
                    }
                    DataRow exceptionRow = exceptionTable.NewRow();

                    exceptionRow["渠道"] = "01";
                    exceptionRow["客户名称"] = "乐友";
                    exceptionRow["订单日期"] = ((DateTime)orderRow["制单日期"]).ToString("MM/dd/yyyy");
                    exceptionRow["客户PO"] = itemRow["order_id"];
                    exceptionRow["雀巢 SAP PO"] = GetNestlePONo(orderRow);
                    exceptionRow["客户产品代码"] = itemRow["乐友货号"];
                    exceptionRow["雀巢产品代码"] = GetNestleMaterialNo(itemRow);
                    exceptionRow["产品名称"] = itemRow["产品描述"];
                    exceptionRow["BU"] = GetBU(itemRow);
                    exceptionRow["数量"] = itemRow["数量"];
                    exceptionRow["客户价格"] = itemRow["单价"];
                    exceptionRow["雀巢价格"] = GetNestle_NPS(itemRow);
                    exceptionRow["客户箱规"] = GetSpec(itemRow);
                    exceptionRow["雀巢箱规"] = GetSpec(itemRow);
                    exceptionRow["单价价差"] = CalcPriceDiffPerCS(itemRow);
                    exceptionRow["产品行价差"] = itemRow["PriceDiff"];
                    exceptionRow["订单总金额价差"] = orderRow["PriceDiff"];
                    exceptionRow["交货地"] = GetPlantNo(orderRow);
                    exceptionRow["客户要求送货日"] = ((DateTime)orderRow["有效时间_end"]).ToString("MM/dd/yyyy");
                    exceptionRow["物流模式/订单类型"] = string.Empty;
                    exceptionRow["问题分类"] = GetIssues(itemRow, orderRow);
                    exceptionRow["问题详细描述"] = GetIssueDetail(itemRow, orderRow);
                    exceptionRow["备注"] = orderRow["备注"];

                    exceptionTable.Rows.Add(exceptionRow);
                }
            }

            return exceptionTable;
        }

        public System.Data.DataTable GenExcelToTable()
        {
            System.Data.DataTable exceltoTable = TableInit.GetExceltoTable();
            foreach (DataRow orderRow in OrderTable.Rows)
            {
                System.Data.DataTable ItemTable = (System.Data.DataTable)orderRow["ItemTable"];
                foreach (DataRow itemRow in ItemTable.Rows)
                {
                    int index = ItemTable.Rows.IndexOf(itemRow);
                    DataRow exceltoRow = exceltoTable.NewRow();

                    exceltoRow["Order Type"] = "OR";
                    exceltoRow["Sales Org"] = "CN26";
                    exceltoRow["Distribution channel"] = "01";
                    exceltoRow["Sold to"] = GetSoldto(orderRow);
                    exceltoRow["Ship to"] = GetShipto(orderRow);
                    exceltoRow["PO"] = index + 1;
                    exceltoRow["PO Number"] = GetNestlePONo(orderRow);
                    exceltoRow["Reqd Del Date"] = ((DateTime)orderRow["有效时间_end"]).ToString("yyyyMMdd");
                    exceltoRow["SAP Material"] = GetNestleMaterialNo(itemRow);
                    exceltoRow["Qty"] = (GetSpec(itemRow) != 0) ? Convert.ToInt32(itemRow["数量"]) / GetSpec(itemRow) : 0;
                    exceltoRow["UoM"] = "CS";

                    exceltoTable.Rows.Add(exceltoRow);
                }
            }
            return exceltoTable;
        }
    }

public static class Issues
    {
        public static readonly Dictionary<IssuesType, string> issuesDisc = new Dictionary<IssuesType, string>()
        {
            {IssuesType.订单价格差异, "订单不符合价差范围"},
            {IssuesType.非整数订单, "订单的数量不为整数"},
            {IssuesType.特殊产品订单,"含特殊产品订单" },
            {IssuesType.无法mapping雀巢产品,"订单无法获取雀巢编码" },
            {IssuesType.跨月订单, "订单有效期在下个月" },
            {IssuesType.送货地址有误订单,"乐友备注上的送货地址与订单明细表格内的地址不一致" },
            {IssuesType.送货日问题订单,"送货周期不足4天" },
            {IssuesType.负数价格,"客户系统订单价格为负数或零" }
        };

        public enum IssuesType
        {
            订单价格差异,
            非整数订单,
            特殊产品订单,
            无法mapping雀巢产品,
            跨月订单,
            送货地址有误订单,
            送货日问题订单,
            负数价格
        }

        public static string GetAlertMessage(string fileName)
        {
            return $"订单处理失败，{fileName} 文件有问题，云扩人员正在分析问题，请等待进一步通知";
        }
    }

internal static class MailUtility
    {
        static readonly string SenderDisplayName = "Encoo RPA";
        public static void SendResultMail(Newtonsoft.Json.Linq.JObject sendMailObj, string receiverMail, string fileFullName, System.Data.DataTable dataTable, HashSet<string> duplicateList)
        {
            string SMTPServer = sendMailObj["smtpServer"].ToString();
            string MailBoxAddress = sendMailObj["email"].ToString();
            string password = sendMailObj["password"].ToString();
            
            
            //确定smtp服务器地址。实例化一个Smtp客户端
            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(SMTPServer);
            //生成一个发送地址
            client.Port = Convert.ToInt32(sendMailObj["port"]);

            //构造一个发件人地址对象
            MailAddress from = new MailAddress(MailBoxAddress, SenderDisplayName, Encoding.UTF8);
            string[] mailAddress = receiverMail.Split('/');
            //构造一个收件人地址对象
            MailAddress to = new MailAddress(mailAddress[0]);
            //构造一个Email的Message对象
            MailMessage message = new MailMessage(from, to);
            //添加多个收件人
            for (int i = 1; i < receiverMail.Split('/').Length; i++)
            {
                if(mailAddress[i] != string.Empty){
                    message.To.Add(mailAddress[i]);
                }                
            }
            //为 message 添加附件
            //得到文件名

            if (dataTable.Rows.Count > 0)
            {

                //判断文件是否存在
                if (File.Exists(fileFullName))
                {
                    //构造一个附件对象
                    Attachment attach = new Attachment(fileFullName);
                    //得到文件的信息
                    ContentDisposition disposition = attach.ContentDisposition;
                    disposition.CreationDate = System.IO.File.GetCreationTime(fileFullName);
                    disposition.ModificationDate = System.IO.File.GetLastWriteTime(fileFullName);
                    disposition.ReadDate = System.IO.File.GetLastAccessTime(fileFullName);
                    //向邮件添加附件
                    message.Attachments.Add(attach);
                }
            }

            //添加邮件主题和内容
            message.Subject = GenerateMailSubject(dataTable);
            message.SubjectEncoding = Encoding.UTF8;
            message.Body = GenerateMailBody(dataTable, duplicateList);
            message.BodyEncoding = Encoding.UTF8;

            //设置邮件的信息
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            message.IsBodyHtml = false;

            //如果服务器支持安全连接，则将安全连接设为true。
            client.EnableSsl = true;
            //设置用户名和密码。
            //string userState = message.Subject;
            client.UseDefaultCredentials = false;
            string username = MailBoxAddress;
            string passwd = password;
            //用户登陆信息
            NetworkCredential myCredentials = new NetworkCredential(username, passwd);
            client.Credentials = myCredentials;
            //发送邮件
            client.Send(message);
        }

        public static void SendErrorMail(Newtonsoft.Json.Linq.JObject sendMailObj, string receiverMail, string errorInfo)
        {
            string SMTPServer = sendMailObj["smtpServer"].ToString();
            string MailBoxAddress = sendMailObj["email"].ToString();
            string password = sendMailObj["password"].ToString();
            
            //确定smtp服务器地址。实例化一个Smtp客户端
            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(SMTPServer);
            //生成一个发送地址


            //构造一个发件人地址对象
            MailAddress from = new MailAddress(MailBoxAddress, SenderDisplayName, Encoding.UTF8);
            //构造一个收件人地址对象
            MailAddress to = new MailAddress(receiverMail);

            //构造一个Email的Message对象
            MailMessage message = new MailMessage(from, to);


            //添加邮件主题和内容
            message.Subject = "RPA错误报告";
            message.SubjectEncoding = Encoding.UTF8;
            message.Body = errorInfo;
            message.BodyEncoding = Encoding.UTF8;

            //设置邮件的信息
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            message.IsBodyHtml = false;

            //如果服务器支持安全连接，则将安全连接设为true。
            client.EnableSsl = true;
            client.Port = Convert.ToInt32(sendMailObj["port"]);
            //设置用户名和密码。
            client.UseDefaultCredentials = false;
            NetworkCredential myCredentials = new NetworkCredential(MailBoxAddress, password);
            client.Credentials = myCredentials;
            //发送邮件
            client.Send(message);
        }

        private static string GenerateMailSubject(System.Data.DataTable datatable)
        {
            switch (datatable.TableName)
            {
                case "CleanTable":
                    return "雀巢乐友Clean order list";
                case "ExceptionTable":
                    return "雀巢乐友Exception order list";
                case "ExcelToTable":
                    return "雀巢乐友Ex2o list";
                default:
                    return null;
            }
        }

        private static string GenerateMailBody(System.Data.DataTable datatable, HashSet<string> duplicateList)
        {
            switch (datatable.TableName)
            {
                case "CleanTable" when duplicateList.Count == 0:
                    return GenerateCleanTableMailBody(datatable);
                case "CleanTable" when duplicateList.Count > 0:
                    return AppendDuplicateList(GenerateCleanTableMailBody(datatable), duplicateList);
                case "ExceptionTable" when duplicateList.Count == 0:
                    return GenerateExceptionTalbeMailBody(datatable);
                case "ExceptionTable" when duplicateList.Count > 0:
                    return AppendDuplicateList(GenerateExceptionTalbeMailBody(datatable), duplicateList);
                case "ExcelToTable" when duplicateList.Count == 0:
                    return GenerateExceltoTalbeMailBody(datatable);
                case "ExcelToTable" when duplicateList.Count > 0:
                    return AppendDuplicateList(GenerateExceltoTalbeMailBody(datatable), duplicateList);
                default:
                    return null;
            }
        }

        private static string AppendDuplicateList(string origin, HashSet<string> duplicateList)
        {
            StringBuilder sb = new StringBuilder(origin);
            sb.AppendLine("以下为重复订单 ：");
            foreach (string duplicate in duplicateList)
            {
                sb.AppendLine(duplicate);
            }
            return sb.ToString();
        }
        
        private static string GenerateCleanTableMailBody(System.Data.DataTable dataTable)
        {
            string res = string.Empty;
            if (dataTable.Rows.Count == 0)
            {
                res = @"Dear All，
                            本时段乐友系统无新增clean order，请知悉，谢谢。";
            }
            else
            {
                res = @"Dear All，
                            附件为本时段乐友clean order list，请参协助处理，谢谢。";
            }
            return res;
        }

        private static string GenerateExceptionTalbeMailBody(System.Data.DataTable dataTable)
        {
            string res = string.Empty;
            if (dataTable.Rows.Count == 0)
            {
                res = @"Dear All，
                            本时段乐友系统无新增exception order，请知悉，谢谢。";
            }
            else
            {
                res = @"Dear All，
                            附件为本时段乐友exception order list，请参协助处理，谢谢。";
            }
            return res;
        }

        private static string GenerateExceltoTalbeMailBody(System.Data.DataTable dataTable)
        {
            string res = string.Empty;
            if (dataTable.Rows.Count == 0)
            {
                res = @"Dear All，
                            本时段乐友系统无新增excel to order list，请知悉，谢谢。";
            }
            else
            {
                res = @"Dear All，
                            附件为本时段乐友excel to order list，请参协助处理，谢谢。";
            }
            return res;
        }
    }

public class OrderReader
    {
        HtmlAgilityPack.HtmlDocument Doc = new HtmlAgilityPack.HtmlDocument();

        Dictionary<string, string> OrderXPath = new Dictionary<string, string>()
        {
            {"PO单号","/html/body/table[2]/tbody/tr[1]/td[2]/b"},
            {"目的库名称","/html/body/table[2]/tbody/tr[1]/td[4]/b"},
            {"制单日期","/html/body/table[2]/tbody/tr[3]/td[2]/b"},
            {"有效时间","/html/body/table[2]/tbody/tr[3]/td[4]/b" },
            {"PO中心库房最早收货时间","/html/body/table[2]/tbody/tr[4]/td[2]/b"},
            {"供应商号","/html/body/table[2]/tbody/tr[5]/td[2]/b"},
            {"PO单类型","/html/body/table[2]/tbody/tr[5]/td[4]/b"},
            {"名称","/html/body/table[2]/tbody/tr[7]/td[2]"},
            {"地址","/html/body/table[2]/tbody/tr[8]/td[2]"},
            {"联系人","/html/body/table[2]/tbody/tr[9]/td[2]"},
            {"电话","/html/body/table[2]/tbody/tr[10]/td[2]" },
            {"传真" ,"/html/body/table[2]/tbody/tr[11]/td[2]"},
            {"邮编" ,"/html/body/table[2]/tbody/tr[12]/td[2]"},
            {"采购及结算公司" ,"/html/body/table[2]/tbody/tr[7]/td[4]"},
            {"供应商送货地址" ,"/html/body/table[2]/tbody/tr[8]/td[4]"},
            {"采购商联系人" ,"/html/body/table[2]/tbody/tr[9]/td[4]"},
            {"采购商电话" ,"/html/body/table[2]/tbody/tr[10]/td[4]"},
            {"采购商传真" ,"/html/body/table[2]/tbody/tr[11]/td[4]"},
            {"采购商邮编" ,"/html/body/table[2]/tbody/tr[12]/td[4]"},
            {"备注","/html/body/table[3]/tbody/tr/td/text()"},
            {"总额","/html/body/table[4]/tbody/tr[last()]/td[last()]"}
        };

        public OrderReader()
        {

        }

        public DataRow GetOrderRow()
        {
            System.Data.DataTable orderTable = TableInit.GetOrderTable();

            DataRow dataRow = orderTable.NewRow();

            foreach (var item in OrderXPath)
            {
                if (orderTable.Columns[item.Key].DataType == typeof(DateTime))
                {
                    if (orderTable.Columns[item.Key].ColumnName == "有效时间")
                    {
                        string theDateStr = GetContentByXPath(item.Value);
                        string startDateStr = theDateStr.Trim().Split('―')[0];
                        string endDateStr = theDateStr.Trim().Split('―')[1];

                        DateTime startDate;
                        DateTime endDate;
                        if (DateTime.TryParse(startDateStr, out startDate))
                        {
                            dataRow["有效时间_start"] = startDate;
                        }
                        if (DateTime.TryParse(endDateStr, out endDate))
                        {
                            dataRow["有效时间_end"] = endDate;
                        }
                    }
                    else
                    {
                        DateTime theDate;
                        DateTime.TryParse(GetContentByXPath(item.Value), out theDate);
                        dataRow[item.Key] = theDate;
                    }
                }
                else
                {
                    dataRow[item.Key] = GetContentByXPath(item.Value);
                }
            }

            dataRow["Created_time"] = DateTime.Now;
            //dataRow["Created_time"] = new DateTime(2021, 12, 15);
            dataRow["ItemTable"] = GetItemTable();
            return dataRow;
        }

        public System.Data.DataTable GetItemTable()
        {
            System.Data.DataTable itemTable = TableInit.GetItemTable();
            DataRow itemRow = itemTable.NewRow();
            HtmlNodeCollection itemTableNodes = Doc.DocumentNode.SelectNodes("/html/body/table[4]/tbody/tr");
            foreach (HtmlNode node in itemTableNodes)
            {
                int index = itemTableNodes.IndexOf(node);
                if (index % 2 != 0 && index != itemTableNodes.Count - 1)
                {
                    List<string> rowsArr = node.SelectNodes("td").Select(td => HttpUtility.HtmlDecode(td.InnerText).Trim()).ToArray().ToList();
                    rowsArr.Add(GetContentByXPath(OrderXPath["PO单号"]));
                    itemTable.Rows.Add(rowsArr.ToArray());
                }
            }

            return itemTable;
        }
        public string GetContentByXPath(string xPath)
        {
            string res = Doc.DocumentNode
            .SelectSingleNode(xPath)
            .InnerText;
            return HttpUtility.HtmlDecode(res).Trim();
        }

        public void LoadHtmlFromFileFullName(string fileFullName)
        {
            Doc.OptionDefaultStreamEncoding = Encoding.GetEncoding("GB2312");
            Doc.DetectEncodingAndLoad(fileFullName, true);
        }
    }

internal static class SaveExcelFile
    {
        public static string SaveDataTable2Excel(System.Data.DataTable dt, string fileName)
        {
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;

            string filePathstr = Program.SaveExcelFolderPathStr + fileName + "_" + System.DateTime.Now.ToString("yyyy-MM-dd") + @".xlsx";

            if (!System.IO.Directory.Exists(Program.SaveExcelFolderPathStr))
            {
                System.IO.Directory.CreateDirectory(Program.SaveExcelFolderPathStr);
            }

            if (System.IO.File.Exists(filePathstr))
            {
                System.IO.File.Delete(filePathstr);
            }

            Microsoft.Office.Interop.Excel.Application appexcel = new Microsoft.Office.Interop.Excel.Application();

            System.Reflection.Missing miss = System.Reflection.Missing.Value;

            appexcel = new Microsoft.Office.Interop.Excel.Application();

            Microsoft.Office.Interop.Excel.Workbook workbookdata;

            Microsoft.Office.Interop.Excel.Worksheet worksheetdata;

            Microsoft.Office.Interop.Excel.Range range;



            //设置对象不可见

            appexcel.Visible = false;

            System.Globalization.CultureInfo currentci = System.Threading.Thread.CurrentThread.CurrentCulture;

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");

            workbookdata = appexcel.Workbooks.Add(miss);

            worksheetdata = (Microsoft.Office.Interop.Excel.Worksheet)workbookdata.Worksheets.Add(miss, miss, miss, miss);

            //给工作表赋名称
            worksheetdata.Name = dt.TableName;

            int startRow = 2;
            //ExcelToTable 表头及格式
            if (worksheetdata.Name == "ExcelToTable")
            {
                //从A3开始填充数据
                startRow = 3;
                //excelToTable 表头
                string[] exceltoheader = System.Linq.Enumerable.Repeat(string.Empty, 30).ToArray();
                exceltoheader[0] = "OR = sales order /ZSAD = sample"; exceltoheader[1] = "M"; exceltoheader[2] = "M"; exceltoheader[3] = "M"; exceltoheader[5] = "M";exceltoheader[17] = "ZSAD --> M";
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    worksheetdata.Cells[1, i + 1] = exceltoheader[i];
                }
                //列名表头
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i == 7)
                    {
                        worksheetdata.Cells[2, i + 1] = @"=CONCATENATE(""PO: "",COUNTIF(H3:H243,""= 1""))";
                    }
                    else
                    {
                        worksheetdata.Cells[2, i + 1] = dt.Columns[i].ColumnName.ToString();
                    }
                }
                worksheetdata.Range[appexcel.Cells[2, 1], appexcel.Cells[2, 4]].Interior.ColorIndex = 6;
                worksheetdata.Range[appexcel.Cells[2, 9], appexcel.Cells[2, 11]].Interior.ColorIndex = 6;
                worksheetdata.Range[appexcel.Cells[2, 13], appexcel.Cells[2, 14]].Interior.ColorIndex = 6;
                worksheetdata.Range[appexcel.Cells[2, 8], appexcel.Cells[2, 8]].Interior.ColorIndex = 3;
                worksheetdata.Range[appexcel.Cells[2, 1], appexcel.Cells[2, 30]].Font.Bold = true;
                worksheetdata.get_Range(appexcel.Cells[startRow, 3], appexcel.Cells[rowCount + startRow - 1, 3]).NumberFormatLocal = "@";
            }
            else
            {
                //列名表头
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    worksheetdata.Cells[startRow - 1, i + 1] = dt.Columns[i].ColumnName.ToString();
                }
                //第一列设置文本格式 #B4C6E7
                worksheetdata.get_Range(appexcel.Cells[startRow, 1], appexcel.Cells[rowCount + startRow - 1, 1]).NumberFormatLocal = "@";
                worksheetdata.Range[appexcel.Cells[1, 1], appexcel.Cells[1, colCount]].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(180, 198, 231));
                worksheetdata.Range[appexcel.Cells[1, 1], appexcel.Cells[1, colCount]].Font.Bold = true;
            }

            //写入数据

            object[,] data = new object[rowCount, colCount];
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                {
                    data[rowIndex, colIndex] = dt.Rows[rowIndex][colIndex].ToString();
                }
            }

            range = worksheetdata.get_Range(appexcel.Cells[startRow, 1], appexcel.Cells[rowCount + startRow - 1, colCount]);
            
            range.Value2 = data;
            worksheetdata.Columns.AutoFit();

            //调用方法关闭excel进程
            workbookdata.SaveAs(Filename: filePathstr);

            workbookdata.Close();
            appexcel.Quit();
            //appexcel.Visible = true;
            return filePathstr;
        }
    }

public static class TableInit
    {
        public static System.Data.DataTable GetOrderTable()
        {
            System.Data.DataTable OrderTable = new System.Data.DataTable("OrderTable");

            OrderTable.Columns.Add("PO单号", typeof(string));
            OrderTable.Columns.Add("目的库名称", typeof(string));
            OrderTable.Columns.Add("制单日期", typeof(DateTime));
            OrderTable.Columns.Add("有效时间", typeof(DateTime));  //Remove in DataBase
            OrderTable.Columns.Add("有效时间_start", typeof(DateTime));
            OrderTable.Columns.Add("有效时间_end", typeof(DateTime));
            OrderTable.Columns.Add("PO中心库房最早收货时间", typeof(DateTime));
            OrderTable.Columns.Add("供应商号", typeof(string));
            OrderTable.Columns.Add("PO单类型", typeof(string));
            OrderTable.Columns.Add("名称", typeof(string));
            OrderTable.Columns.Add("地址", typeof(string));
            OrderTable.Columns.Add("联系人", typeof(string));
            OrderTable.Columns.Add("电话", typeof(string));
            OrderTable.Columns.Add("传真", typeof(string));
            OrderTable.Columns.Add("邮编", typeof(string));
            OrderTable.Columns.Add("采购及结算公司", typeof(string));
            OrderTable.Columns.Add("供应商送货地址", typeof(string));
            OrderTable.Columns.Add("采购商联系人", typeof(string));
            OrderTable.Columns.Add("采购商电话", typeof(string));
            OrderTable.Columns.Add("采购商传真", typeof(string));
            OrderTable.Columns.Add("采购商邮编", typeof(string));
            OrderTable.Columns.Add("备注", typeof(string));
            OrderTable.Columns.Add("总额", typeof(decimal));

            OrderTable.Columns.Add("Created_time", typeof(DateTime));
            OrderTable.Columns.Add("NumsByCS");
            OrderTable.Columns.Add("PriceDiff", typeof(decimal));
            OrderTable.Columns.Add("Issues");

            OrderTable.Columns.Add("ItemTable", typeof(System.Data.DataTable));//Remove in DataBase

            return OrderTable;
        }

        public static System.Data.DataTable GetItemTable()
        {
            System.Data.DataTable itemTable = new System.Data.DataTable();

            itemTable.Columns.Add("乐友货号");
            itemTable.Columns.Add("厂家货号");
            itemTable.Columns.Add("货位");
            itemTable.Columns.Add("临时货位");
            itemTable.Columns.Add("产品描述");
            itemTable.Columns.Add("单价", typeof(decimal));
            itemTable.Columns.Add("数量", typeof(int));
            itemTable.Columns.Add("实收数量");
            itemTable.Columns.Add("金额小计", typeof(decimal));

            itemTable.Columns.Add("order_id");
            itemTable.Columns.Add("PriceDiff", typeof(decimal));
            itemTable.Columns.Add("Issues");

            return itemTable;
        }

        public static System.Data.DataTable GetCleanTable()
        {
            System.Data.DataTable cleanTable = new System.Data.DataTable("CleanTable");

            cleanTable.Columns.Add("渠道");
            cleanTable.Columns.Add("读单当天日期");
            cleanTable.Columns.Add("客户名称");
            cleanTable.Columns.Add("雀巢PO No");
            cleanTable.Columns.Add("客户系统PO No");
            cleanTable.Columns.Add("订单数量(单位CS)");
            cleanTable.Columns.Add("Plant/区域");
            cleanTable.Columns.Add("交货地");
            cleanTable.Columns.Add("客户要求送货日");
            cleanTable.Columns.Add("实际配送时间");
            cleanTable.Columns.Add("订单有效期截至日期");
            cleanTable.Columns.Add("订单类型");
            cleanTable.Columns.Add("档期");
            cleanTable.Columns.Add("备注");

            return cleanTable;
        }

        public static System.Data.DataTable GetExceptionTable()
        {
            System.Data.DataTable exceptionTable = new System.Data.DataTable("ExceptionTable");

            exceptionTable.Columns.Add("渠道");
            exceptionTable.Columns.Add("客户名称");
            exceptionTable.Columns.Add("订单日期");
            exceptionTable.Columns.Add("客户PO");
            exceptionTable.Columns.Add("雀巢 SAP PO");
            exceptionTable.Columns.Add("客户产品代码");
            exceptionTable.Columns.Add("雀巢产品代码");
            exceptionTable.Columns.Add("产品名称");
            exceptionTable.Columns.Add("BU");
            exceptionTable.Columns.Add("数量");
            exceptionTable.Columns.Add("客户价格");
            exceptionTable.Columns.Add("雀巢价格");
            exceptionTable.Columns.Add("客户箱规");
            exceptionTable.Columns.Add("雀巢箱规");
            exceptionTable.Columns.Add("单价价差");
            exceptionTable.Columns.Add("产品行价差");
            exceptionTable.Columns.Add("订单总金额价差");
            exceptionTable.Columns.Add("交货地");
            exceptionTable.Columns.Add("客户要求送货日");
            exceptionTable.Columns.Add("物流模式/订单类型");
            exceptionTable.Columns.Add("问题分类");
            exceptionTable.Columns.Add("问题详细描述");
            exceptionTable.Columns.Add("备注");

            return exceptionTable;
        }

        public static System.Data.DataTable GetExceltoTable()
        {
            System.Data.DataTable excelToTable = new System.Data.DataTable("ExcelToTable");

            excelToTable.Columns.Add("Order Type");
            excelToTable.Columns.Add("Sales Org");
            excelToTable.Columns.Add("Distribution channel");
            excelToTable.Columns.Add("Sold to");
            excelToTable.Columns.Add("Sold to Name");
            excelToTable.Columns.Add("Ship to");
            excelToTable.Columns.Add("Ship to Name");
            excelToTable.Columns.Add("PO");
            excelToTable.Columns.Add("PO Number");
            excelToTable.Columns.Add("Reqd Del Date");
            excelToTable.Columns.Add("SAP Material");
            excelToTable.Columns.Add("SAP Description");
            excelToTable.Columns.Add("Qty");
            excelToTable.Columns.Add("UoM");
            excelToTable.Columns.Add("SLoc");
            excelToTable.Columns.Add("Batch");
            excelToTable.Columns.Add("Plant");
            excelToTable.Columns.Add("WBS");
            excelToTable.Columns.Add("Item Category");
            excelToTable.Columns.Add("Delivery note text");
            excelToTable.Columns.Add("Language");
            excelToTable.Columns.Add("Order Reason");
            excelToTable.Columns.Add("Cost Center");
            excelToTable.Columns.Add("OTC Name");
            excelToTable.Columns.Add("OTC Street");
            excelToTable.Columns.Add("OTC City Name");
            excelToTable.Columns.Add("Route");
            excelToTable.Columns.Add("OTC Check");

            return excelToTable;
        }

        public static System.Data.DataTable GetOrderTable4DB(System.Data.DataTable orderTable)
        {
            System.Data.DataTable resTable = orderTable.Copy();

            resTable.Columns.Remove("有效时间");
            resTable.Columns.Remove("ItemTable");
            resTable.Columns.Add("id").SetOrdinal(0);

            return resTable;
        }

        public static System.Data.DataTable GetItemTable4DB(System.Data.DataTable itemTable)
        {
            System.Data.DataTable itemTable4DB = itemTable.Copy();
            itemTable4DB.Columns.Add("id").SetOrdinal(0);

            List<string> numList = new List<string>();
            foreach (DataRow row in itemTable4DB.Rows)
            {
                numList.Add(row["实收数量"].ToString());
            }
            itemTable4DB.Columns.Remove("实收数量");
            itemTable4DB.Columns.Add("实收数量", typeof(int));
            foreach (DataRow row in itemTable4DB.Rows)
            {
                if (numList[itemTable4DB.Rows.IndexOf(row)] != String.Empty)
                {
                    row["实收数量"] = Convert.ToInt32(numList[itemTable4DB.Rows.IndexOf(row)]);
                }
            }
            return itemTable4DB;
        }

        public static System.Data.DataTable GetCleanTable4DB(System.Data.DataTable cleanTable)
        {
            System.Data.DataTable cleanTable4DB = new System.Data.DataTable();
            cleanTable4DB.Columns.Add("id").SetOrdinal(0);
            cleanTable4DB.Columns.Add("渠道");
            cleanTable4DB.Columns.Add("读单日期", typeof(DateTime));
            cleanTable4DB.Columns.Add("客户名称");
            cleanTable4DB.Columns.Add("雀巢PO_No");
            cleanTable4DB.Columns.Add("客户Po_No");
            cleanTable4DB.Columns.Add("订单数量");
            cleanTable4DB.Columns.Add("区域");
            cleanTable4DB.Columns.Add("交货地");
            cleanTable4DB.Columns.Add("要求送货日", typeof(DateTime));
            cleanTable4DB.Columns.Add("配送时间");
            cleanTable4DB.Columns.Add("有效期", typeof(DateTime));
            cleanTable4DB.Columns.Add("订单类型");
            cleanTable4DB.Columns.Add("档期");
            cleanTable4DB.Columns.Add("备注");
            cleanTable4DB.Columns.Add("created_time", typeof(DateTime));

            foreach (DataRow row in cleanTable.Rows)
            {
                DataRow insertRow = cleanTable4DB.NewRow();

                insertRow["渠道"] = row["渠道"];
                insertRow["读单日期"] = row["读单当天日期"];
                insertRow["客户名称"] = row["客户名称"];
                insertRow["雀巢PO_No"] = row["雀巢PO No"];
                insertRow["客户Po_No"] = row["客户系统PO No"];
                insertRow["订单数量"] = row["订单数量(单位CS)"];
                insertRow["区域"] = row["Plant/区域"];
                insertRow["交货地"] = row["交货地"];
                insertRow["要求送货日"] = row["客户要求送货日"];
                insertRow["配送时间"] = row["实际配送时间"];
                insertRow["有效期"] = row["订单有效期截至日期"];
                insertRow["订单类型"] = row["订单类型"];
                insertRow["档期"] = row["档期"];
                insertRow["备注"] = row["备注"];
                insertRow["created_time"] = DateTime.Now;

                cleanTable4DB.Rows.Add(insertRow);
            }
            return cleanTable4DB;
        }

        public static System.Data.DataTable GetExceptionTable4DB(System.Data.DataTable exceptionTable)
        {
            System.Data.DataTable exceptionTable4DB = new System.Data.DataTable();

            exceptionTable4DB.Columns.Add("id");
            exceptionTable4DB.Columns.Add("渠道");
            exceptionTable4DB.Columns.Add("客户名称");
            exceptionTable4DB.Columns.Add("订单日期", typeof(DateTime));
            exceptionTable4DB.Columns.Add("客户PO");
            exceptionTable4DB.Columns.Add("客户产品码");
            exceptionTable4DB.Columns.Add("雀巢产品码");
            exceptionTable4DB.Columns.Add("区域");
            exceptionTable4DB.Columns.Add("产品名称");
            exceptionTable4DB.Columns.Add("Nestle_BU");
            exceptionTable4DB.Columns.Add("数量");
            exceptionTable4DB.Columns.Add("SAP_PO");
            exceptionTable4DB.Columns.Add("C4开票价");
            exceptionTable4DB.Columns.Add("价差");
            exceptionTable4DB.Columns.Add("箱价");
            exceptionTable4DB.Columns.Add("TPP扣点");
            exceptionTable4DB.Columns.Add("实际扣点");
            exceptionTable4DB.Columns.Add("客户要求送货日", typeof(DateTime));
            exceptionTable4DB.Columns.Add("客户价格");
            exceptionTable4DB.Columns.Add("雀巢价格");
            exceptionTable4DB.Columns.Add("单价价差");
            exceptionTable4DB.Columns.Add("产品行价差");
            exceptionTable4DB.Columns.Add("订单总金额价差");
            exceptionTable4DB.Columns.Add("交货地");
            exceptionTable4DB.Columns.Add("雀巢箱规");
            exceptionTable4DB.Columns.Add("客户箱规");
            exceptionTable4DB.Columns.Add("起送日", typeof(DateTime));
            exceptionTable4DB.Columns.Add("物流模式");
            exceptionTable4DB.Columns.Add("问题分类");
            exceptionTable4DB.Columns.Add("问题详细描述");
            exceptionTable4DB.Columns.Add("备注");
            exceptionTable4DB.Columns.Add("created_time", typeof(DateTime));

            foreach (DataRow row in exceptionTable.Rows)
            {
                DataRow insertRow = exceptionTable4DB.NewRow();

                //insertRow["id"] = ;
                insertRow["渠道"] = row["渠道"];
                insertRow["客户名称"] = row["客户名称"];
                insertRow["订单日期"] = Convert.ToDateTime(row["订单日期"]);
                insertRow["客户PO"] = row["客户PO"];
                insertRow["客户产品码"] = row["客户产品代码"];
                insertRow["雀巢产品码"] = row["雀巢产品代码"];
                //insertRow["区域"] = ;
                insertRow["产品名称"] = row["产品名称"];
                insertRow["Nestle_BU"] = row["BU"];
                insertRow["数量"] = row["数量"];
                insertRow["SAP_PO"] = row["雀巢 SAP PO"];
                //insertRow["C4开票价"] = ;
                insertRow["价差"] = row["单价价差"];
                //insertRow["箱价"] = ;
                //insertRow["TPP扣点"] = ;
                //insertRow["实际扣点"] = ;
                insertRow["客户要求送货日"] = row["客户要求送货日"];
                insertRow["客户价格"] = row["客户价格"];
                insertRow["雀巢价格"] = row["雀巢价格"];
                //insertRow["单价价差"] = ;
                insertRow["产品行价差"] = row["产品行价差"];
                insertRow["订单总金额价差"] = row["订单总金额价差"];
                insertRow["交货地"] = row["交货地"];
                insertRow["雀巢箱规"] = row["雀巢箱规"];
                insertRow["客户箱规"] = row["客户箱规"];
                //insertRow["起送日"] = ;
                insertRow["物流模式"] = row["物流模式/订单类型"];
                insertRow["问题分类"] = row["问题分类"];
                insertRow["问题详细描述"] = row["问题详细描述"];
                insertRow["备注"] = row["备注"];
                insertRow["created_time"] = DateTime.Now;

                exceptionTable4DB.Rows.Add(insertRow);
            }

            return exceptionTable4DB;
        }

        public static System.Data.DataTable GetExceltoTable4DB(System.Data.DataTable exceltoTable)
        {
            System.Data.DataTable exceltoTable4DB = new System.Data.DataTable();
            exceltoTable4DB.Columns.Add("id").SetOrdinal(0);
            exceltoTable4DB.Columns.Add("Customer_Order_Date", typeof(DateTime));
            exceltoTable4DB.Columns.Add("Customer_Name");
            exceltoTable4DB.Columns.Add("Sales_Order_Type");
            exceltoTable4DB.Columns.Add("Sales_Org");
            exceltoTable4DB.Columns.Add("Distribution_channel");
            exceltoTable4DB.Columns.Add("Sold_to");
            exceltoTable4DB.Columns.Add("Sold_to_Name");
            exceltoTable4DB.Columns.Add("Ship_to");
            exceltoTable4DB.Columns.Add("Ship_to_Name");
            exceltoTable4DB.Columns.Add("PO");
            exceltoTable4DB.Columns.Add("PO_Number");
            exceltoTable4DB.Columns.Add("Reqd_Del_Date", typeof(DateTime));
            exceltoTable4DB.Columns.Add("SAP_Material");
            exceltoTable4DB.Columns.Add("SAP_Description");
            exceltoTable4DB.Columns.Add("Qty");
            exceltoTable4DB.Columns.Add("UoM");
            exceltoTable4DB.Columns.Add("SLoc");
            exceltoTable4DB.Columns.Add("Batch");
            exceltoTable4DB.Columns.Add("Plant");
            exceltoTable4DB.Columns.Add("WBS");
            exceltoTable4DB.Columns.Add("Item_Category");
            exceltoTable4DB.Columns.Add("Delivery_note_text");
            exceltoTable4DB.Columns.Add("Language");
            exceltoTable4DB.Columns.Add("Order_Reason");
            exceltoTable4DB.Columns.Add("Cost_Center");
            exceltoTable4DB.Columns.Add("OTC_Name");
            exceltoTable4DB.Columns.Add("OTC_Street");
            exceltoTable4DB.Columns.Add("OTC_City_Name");
            exceltoTable4DB.Columns.Add("Route");
            exceltoTable4DB.Columns.Add("OTC_Check");
            exceltoTable4DB.Columns.Add("created_time", typeof(DateTime));
            exceltoTable4DB.Columns.Add("Item_Condition_Type");
            exceltoTable4DB.Columns.Add("Item_Condition_Value");
            exceltoTable4DB.Columns.Add("Item_Condition_Type_1");
            exceltoTable4DB.Columns.Add("Item_Condition_Value_1");
            exceltoTable4DB.Columns.Add("Header_Assignment");
            exceltoTable4DB.Columns.Add("Header_Reference");
            exceltoTable4DB.Columns.Add("Spec_stock_partner");
            exceltoTable4DB.Columns.Add("Customer_Order_Number");

            foreach (DataRow row in exceltoTable.Rows)
            {
                DataRow insertRow = exceltoTable4DB.NewRow();
                
                insertRow["Customer_Name"] = "乐友";
                insertRow["Sales_Order_Type"] = row["Order Type"];
                insertRow["Sales_Org"] = row["Sales Org"];
                insertRow["Distribution_channel"] = row["Distribution channel"];
                insertRow["Sold_to"] = row["Sold to"];
                insertRow["Sold_to_Name"] = row["Sold to Name"];
                insertRow["Ship_to"] = row["Ship to"];
                insertRow["Ship_to_Name"] = row["Ship to Name"];
                insertRow["PO"] = row["PO"];
                insertRow["PO_Number"] = row["PO Number"];
                insertRow["Reqd_Del_Date"] = Convert.ToDateTime(row["Reqd Del Date"].ToString().Insert(4, "-").Insert(7, "-"));
                insertRow["SAP_Material"] = row["SAP Material"];
                insertRow["SAP_Description"] = row["SAP Description"];
                insertRow["Qty"] = row["Qty"];
                insertRow["UoM"] = row["UoM"];
                insertRow["SLoc"] = row["SLoc"];
                insertRow["Batch"] = row["Batch"];
                insertRow["Plant"] = row["Plant"];
                insertRow["WBS"] = row["WBS"];
                insertRow["Item_Category"] = row["Item Category"];
                insertRow["Delivery_note_text"] = row["Delivery note text"];
                insertRow["Language"] = row["Language"];
                insertRow["Order_Reason"] = row["Order Reason"];
                insertRow["Cost_Center"] = row["Cost Center"];
                insertRow["OTC_Name"] = row["OTC Name"];
                insertRow["OTC_Street"] = row["OTC Street"];
                insertRow["OTC_City_Name"] = row["OTC City Name"];
                insertRow["Route"] = row["Route"];
                insertRow["OTC_Check"] = row["OTC Check"];
                insertRow["created_time"] = DateTime.Now;
                insertRow["Customer_Name"] = "乐友";
                insertRow["Customer_Order_Number"] = row["PO Number"].ToString() == string.Empty ?  string.Empty : row["PO Number"].ToString().Substring(row["PO Number"].ToString().Length - 7);

                exceltoTable4DB.Rows.Add(insertRow);
            }

            return exceltoTable4DB;
        }
    }