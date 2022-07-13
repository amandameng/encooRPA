//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable orderDT;
    NestleOrder.Run(txtFilePath, out orderDT);

    string 采购单号 = orderDT.Rows[0]["采购单号"].ToString();
   Console.WriteLine("采购单号++:{0}", 采购单号);
    string 订单类型 = "标准采购订单";

    if(采购单号.Contains("越库")){
       订单类型 += " 越库";
    }
    采购单号 = 采购单号.Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries)[0];
    Console.WriteLine("采购单号--:{0} 订单类型：{1}", 采购单号, 订单类型);
    采购单类型字典["采购单号"] = 采购单号;
    //Convert.ToInt16("qqq");
    采购单类型字典["采购单类型"] = 订单类型;
}
//在这里编写您的函数或者类

 class NestleOrder
    {
        // ******************************
        // 订单基础信息
        const String REG_PURCH_NO = "(?<=采购单号)(.+)(?=到货日期)";
        const String REG_ARRI_DATE = "(?<=到货日期)(.+)(?=创建日期)";
        const String REG_CREATE_TIME = "(?<=创建日期)(.+)(?=供应商编码)";
        const String REG_SUPP_CODE = "(?<=供应商编码)(.+)(?=[\u4e00-\u9fa5]+)";
        const String REG_SUPP_CODE_NUM = "(\\d+)";
        const String REG_SUPP_NAME_PRE = "(?<=供应商编码)(.+)(?=供应商名称)";
        const String REG_SUPP_NAME_PRE_STR = "([\u4e00-\u9fa5]+)";
        const String REG_SUPP_NAME_AFT = "(?<=送货联系人)(.+)(?=仓库名称)";
        const String REG_SUPP_NAME_AFT_STR = "([\u4e00-\u9fa5]$)";
        const String REG_STORE_NAME = "(?<=仓库名称)(.+)(?=行)";

        const String PURCH_NO = "采购单号";
        const String ARRI_DATE = "到货日期";
        const String CREATE_DATE = "创建日期";
        const String SUPP_CODE = "供应商编码";
        const String SUPP_NAME = "供应商名称";
        const String STORE_NAME = "仓库名称";

        public static void Run(string txtFilePath, out DataTable orderDT)
        {
            String fileName = txtFilePath; // @"C:\RPA工作目录\雀巢_家乐福\结果输出\2021-10\2021-10-10\雀巢_苏宁家乐福_1027758315.txt";

            long start = DateTime.Now.Ticks / 10000;

            string delimStr = "", nodelimStr = "";
            ReadFile(fileName, ref nodelimStr, ref delimStr);

            orderDT = RegPurchBasic(nodelimStr);
            // RegGoodsList(delimStr);

            long end = DateTime.Now.Ticks / 10000;
            Console.WriteLine("############################\r\ncost_time:{0}ms", end - start);

            Console.ReadLine();
        }

        static void ReadFile(string fileName, ref string noEndDelimeterStr, ref string delimeterStr)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            sr.BaseStream.Seek(0, SeekOrigin.Begin);

            StringBuilder sbNoDelim = new StringBuilder();
            StringBuilder sbDelim = new StringBuilder();
            string str = sr.ReadLine();
            while (str != null)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    sbDelim.Append(str + "\n");
                    sbNoDelim.Append(str.Replace("\n\r", " "));
                }

                str = sr.ReadLine();
            }

            //C#读取TXT文件之关上文件，留心顺序，先对文件内部执行关闭，然后才是文件~   
            sr.Close();
            fs.Close();

            delimeterStr = sbDelim.ToString();
            noEndDelimeterStr = sbNoDelim.ToString();
        }


        static DataTable RegPurchBasic(string fileContent)
        {
            Regex regPurchaseNo = new Regex(REG_PURCH_NO);
            Regex regArriDate = new Regex(REG_ARRI_DATE);
            Regex regCreateTime = new Regex(REG_CREATE_TIME);
            Regex regSuppCode = new Regex(REG_SUPP_CODE);
            Regex regSuppCodeNum = new Regex(REG_SUPP_CODE_NUM);
            Regex regSupNamePre = new Regex(REG_SUPP_NAME_PRE);
            Regex regSupNamePreStr = new Regex(REG_SUPP_NAME_PRE_STR);
            Regex regSupNameAft = new Regex(REG_SUPP_NAME_AFT);
            Regex regSupNameAftStr = new Regex(REG_SUPP_NAME_AFT_STR);
            Regex regStoreName = new Regex(REG_STORE_NAME);

            DataTable dt = new DataTable();
            dt.Columns.Add(PURCH_NO, typeof(String));
            dt.Columns.Add(ARRI_DATE, typeof(String));
            dt.Columns.Add(CREATE_DATE, typeof(String));
            dt.Columns.Add(SUPP_CODE, typeof(String));
            dt.Columns.Add(SUPP_NAME, typeof(String));
            dt.Columns.Add(STORE_NAME, typeof(String));

            DataRow dr = dt.NewRow();

            Match match = regPurchaseNo.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[1].Value;

                //Console.WriteLine("PuchaseNo(b1):{0}", value.Trim());
                dr[0] = value.Trim();
            }

            match = regArriDate.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                //Console.WriteLine("Arrive Date(b2):{0}", value.Trim());

                dr[1] = value.Trim();
            }

            match = regCreateTime.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[0].Value;
                //Console.WriteLine("Create Time(b3):{0}", value.Trim());
                dr[2] = value.Trim();
            }

            match = regSuppCode.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                match = regSuppCodeNum.Match(value);
                if (match.Success)
                {
                    string valueCode = match.Groups[1].Value;
                    //Console.WriteLine("Supply Code(b4):{0}", valueCode.Trim());

                    dr[3] = valueCode.Trim();
                }
            }

            string suppName = "";
            match = regSupNamePre.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[1].Value;

                match = regSupNamePreStr.Match(value.Trim());
                if (match.Success)
                {
                    string valueStr = match.Groups[1].Value;
                    //Console.WriteLine("Supply Name Pre(b5):{0}", valueStr.Trim());

                    suppName += valueStr.Trim();
                }
            }

            match = regSupNameAft.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[1].Value;

                match = regSupNameAftStr.Match(value.Trim());
                if (match.Success)
                {
                    string valueStr = match.Groups[1].Value;
                    //Console.WriteLine("Supply Name Aft(b5):{0}", valueStr.Trim());

                    suppName += valueStr.Trim();
                }
            }

            if (!string.IsNullOrEmpty(suppName))
            {
                dr[4] = suppName.Trim();
            }

            match = regStoreName.Match(fileContent);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                //Console.WriteLine("Store Name(b6):{0}", value.Trim());
                dr[5] = value.Trim();
            }

            dt.Rows.Add(dr);

            foreach (DataRow drow in dt.Rows)
            {
                string value1 = dr[PURCH_NO].ToString();
                string value2 = dr[ARRI_DATE].ToString();
                string value3 = dr[CREATE_DATE].ToString();
                string value4 = dr[SUPP_CODE].ToString();
                string value5 = dr[SUPP_NAME].ToString();
                string value6 = dr[STORE_NAME].ToString();

                Console.WriteLine("{0}:{1};{2}:{3};{4}:{5};{6}:{7};{8}:{9};{10}:{11}",
                   PURCH_NO, value1.Trim(),
                   ARRI_DATE, value2.Trim(),
                   CREATE_DATE, value3.Trim(),
                   SUPP_CODE, value4.Trim(),
                   SUPP_NAME, value5.Trim(),
                   STORE_NAME, value6.Trim());
            }

            return dt;
        }

        // ******************************
        // 订单列表商品
        const String REG_PURCH_LIST = "(?<=箱数)(.+)(?=打印时间)";
        const String REG_PURCH_ITEM = "([\\d|\\S]+)";
        const String REG_PURCH_ITEM_LINE = "([\\d]+)";

        const String LINE = "行号";
        const String ITEM_CODE = "商品编码";
        const String ITEM_NAME = "商品名称";
        const String BARCODE = "商品条形码";
        const String SPEC = "箱规";
        const String BUY_NUM = "采购数量";
        const String BOX_COUNT = "箱数";

        /// <summary>
        /// 解析商品列表
        /// </summary>
        /// <param name="fileContent"></param>
        static void RegGoodsList(string fileContent)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add(LINE, typeof(String));
            dt.Columns.Add(ITEM_CODE, typeof(String));
            dt.Columns.Add(ITEM_NAME, typeof(String));
            dt.Columns.Add(BARCODE, typeof(String));
            dt.Columns.Add(SPEC, typeof(String));
            dt.Columns.Add(BUY_NUM, typeof(String));
            dt.Columns.Add(BOX_COUNT, typeof(String));

            Regex regGoodt = new Regex(REG_PURCH_ITEM);
            Regex regGoodLine = new Regex(REG_PURCH_ITEM_LINE);
            Match match;

            foreach (string str in fileContent.Split('\n'))
            {
                if (string.IsNullOrEmpty(str.Trim()))
                    continue;

                match = regGoodt.Match(str);
                MatchCollection col = regGoodt.Matches(str);
                int cnt = col.Count;

                if (cnt > 0)
                {
                    match = regGoodLine.Match(col[0].Value.Trim());
                    if (!match.Success)
                    {
                        continue;
                    }
                }


                DataRow dr = dt.NewRow();
                if (cnt == 7)
                {
                    dt.Rows.Add(col[0].Value.Trim(),
                        col[1].Value.Trim(),
                        col[2].Value.Trim(),
                        col[3].Value.Trim(),
                        col[4].Value.Trim(),
                        col[5].Value.Trim(),
                        col[6].Value.Trim());
                }
                else if (cnt == 6)
                {
                    dt.Rows.Add(col[0].Value.Trim(),
                        col[1].Value.Trim(),
                        "",
                        col[2].Value.Trim(),
                        col[3].Value.Trim(),
                        col[4].Value.Trim(),
                        col[5].Value.Trim());

                }
            }

            foreach (DataRow dRow in dt.Rows)
            {
                string value1 = dRow[LINE].ToString();
                string value2 = dRow[ITEM_CODE].ToString();
                string value3 = dRow[ITEM_NAME].ToString();
                string value4 = dRow[BARCODE].ToString();
                string value5 = dRow[SPEC].ToString();
                string value6 = dRow[BUY_NUM].ToString();
                string value7 = dRow[BOX_COUNT].ToString();

                Console.WriteLine("{0}:{1};{2}:{3};{4}:{5};{6}:{7};{8}:{9};{10}:{11};{12}:{13}",
                   LINE, value1.Trim(),
                   ITEM_CODE, value2.Trim(),
                   ITEM_NAME, value3.Trim(),
                   BARCODE, value4.Trim(),
                   SPEC, value5.Trim(),
                   BUY_NUM, value6.Trim(),
                   BOX_COUNT, value7.Trim());
            }

        }


    }
