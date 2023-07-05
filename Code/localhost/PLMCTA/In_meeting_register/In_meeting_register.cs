using Aras.IOM;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasScheduleMethodBuildApp.Code
{
    class Wrapper_localhost_PLMCTA_In_meeting_register
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connection">HttpServerConnection</param>
        /// <returns>innMethod</returns>
        public Case_In_meeting_register init(HttpServerConnection connection)
        {
            Case_In_meeting_register innMethod = new Case_In_meeting_register(connection);
            return innMethod;
        }
        public class Case_In_meeting_register : Item
        {
            public Aras.Server.Core.CallContext CCO { get; set; }
            public Aras.Server.Core.IContextState RequestState { get; set; }
            public Case_In_meeting_register(IServerConnection arg) : base(arg)
            {
            }

            /// <summary>
            /// method內容請由這邊填寫
            /// </summary>
            /// <returns>Item</returns>
            /// 

            //MODE
            public Item MethodCode0()
            {
                #region MethodCode
    //2023-04-19 檢查是不是有隱藏段位
    //System.Diagnostics.Debugger.Break();
    
    Innovator inn = this.getInnovator();
    Innosoft.InnovatorHelper _InnH = new Innosoft.InnovatorHelper(inn);
    
    string strDatabaseName = inn.getConnection().GetDatabaseName();
    string strMethodName = "[" + strDatabaseName + "]" + "In_meeting_register";

    string log_file = "[" + strDatabaseName + "]" + "In_meeting_register_" + System.DateTime.Now.ToString("yyyyMMdd");
    CCO.Utilities.WriteDebug(log_file, "dom: " + this.dom.InnerXml);
    
    //處理傳入的各項資料  
    string meeting_id = this.getProperty("meeting_id","no_data");//對應的會議物件編號  
    string in_refuser = this.getProperty("agent_id","no_data");//使用者的業務窗口。
    string param = this.getProperty("parameter","no_data"); //取得傳入的param，是escape過的form-data
    string surveytype = this.getProperty("surveytype","1");//surveytype，因為是報名專用，所以未傳入預設為1(1報名問卷)。修改使用者資料時會傳入1a
    string strMuid = this.getProperty("muid","");	//要update的使用者id
    string isUserId = this.getProperty("isUserId","");//偷藏的User ID
    string isIndId = this.getProperty("isIndId","");//偷藏的Identity ID
    string OnlyInsMUser = this.getProperty("OnlyInsMUser",""); //只新增in_meeting_user
    
    string login_resume_id = this.getProperty("login_resume_id","");
    string login_resume_current_org = this.getProperty("login_resume_current_org","");
    string login_resume_group = this.getProperty("login_resume_group","");
    
    string in_org = this.getProperty("in_org","");
    
    string checkResultMsg = "";
    bool isAdmission = true; 
    
    //是否為人員異動
    string in_exchange = this.getProperty("in_exchange","");
    if (in_exchange == "1")
    {
        return this.apply("in_exchange_meeting_register");
    }
    
    bool boolIsModify=false; //是否為修改資料模式
    
    if(strMuid != "")
    {
        boolIsModify=true;
    }
    
    //單位註冊 meeting_id 陣列
    string[] org_arr = new string[] 
    {
        "83B87AE0033640AA8DBA7AF2CF659479", //道館、訓練站
        "38CAB90DF1274E048520A801948AC65C", //學校社團
        "5F73936711E04DC799CB02587F4FF7E0", //一般團體
    };
    //是否為單位註冊
    bool is_gym_registry = org_arr.Contains(meeting_id); //單位

    string[] pms = param.Split('&');//處理傳入的parameter
    string muid = "";//會議使用者id，判斷是否為口袋名單時用到。
    
    Item applicant = inn.newItem("In_Meeting_User"); // 用來add使用者的資料
    Item itmMeeting;
    Item itmError;
    Item mUser;

    if (meeting_id == "")
    {
        itmError = inn.newError("");
	    itmError.setErrorDetail("賽事 id 不得為空白");
	    return itmError;
    }
    
    
    Dictionary<string,Item> dicWriteBacks=new Dictionary<string,Item>(); //需要回寫回使用者的回答欄位清單  
    StringBuilder stbAddAnswer=new StringBuilder("");//用來處理答案及更新In_Meeting的AML。
    applicant.setProperty("source_id",meeting_id);
    string client_user_index = this.getProperty("client_user_index","1");
    string aml = "";
    string sql = "";
    surveytype="1";
    
    string number_keys = GetNoLevelValues(CCO, strMethodName, inn, meeting_id);
    
    if (strMuid != "")
    {
        sql = "SELECT * FROM IN_MEETING_USER WITH(NOLOCK) WHERE id = '" + strMuid + "'";
        Item itmOldMeetingUser = inn.applySQL(sql);

        if (itmOldMeetingUser.isError() || itmOldMeetingUser.getResult() == "")
        {
            itmError = inn.newError("");
    	    itmError.setErrorDetail("報名者資料已變更，請刷新頁面後再行操作");
    	    return itmError;
        }
        
        string in_verify_result = itmOldMeetingUser.getProperty("in_verify_result", "");
        string in_ass_ver_result = itmOldMeetingUser.getProperty("in_ass_ver_result", "");
        bool is_reject = in_verify_result == "0" || in_ass_ver_result == "0";
        
        if (strMuid != "" && is_reject)
        {
            //退件不刪
            boolIsModify = true;
        }
    }

    if (this.getProperty("in_index","") != "" )
    {
        //第一個才負責全刪
        if (client_user_index == "1")
        {
            //刪除報名結果 同組一起刪
            Item MUsers = GetRegistedMeetingUsers(CCO, strMethodName, inn, meeting_id, number_keys, strMuid);
            
            //lina 2020.07.16: 防止關閉後又異動報名資料
            if (!MUsers.isError() && MUsers.getItemCount() > 0)
            {
                Item first_user = MUsers.getItemByIndex(0);
                CheckGymClosed(CCO, strMethodName, inn, meeting_id, first_user.getProperty("in_creator_sno"), "刪除報名資料");
            }
            
        	for(int i = 0;i< MUsers.getItemCount(); i++)
        	{
        	    Item MUser = MUsers.getItemByIndex(i);
    
        		string strRemoveSurveyAML=@"<AML>
        									<Item type='In_Meeting_Surveys_result' action='delete' where=""in_participant='{#muid}' and in_surveytype='1' ""/>
        								</AML>"
        								.Replace("{#muid}",MUser.getID());
        		Item tmp1 = inn.applyAML(strRemoveSurveyAML);
    
    
        		aml = "<AML>"+
        			"<Item type='in_meeting_resume' action='delete' where=\"in_user='" +  MUser.getID() + "'\">"+
        			"</Item></AML>";
    
        		tmp1 = inn.applyAML(aml);
    
                aml = "<AML>"+
                "<Item type='in_meeting_record' action='delete' where=\"in_participant='" +  MUser.getID() + "'\">"+
                "</Item></AML>";
    
                tmp1 = inn.applyAML(aml);
    
    
        // 		aml = "<AML>"+
        // 			"<Item type='in_meeting_user' action='delete' id='" + MUser.getID() + "'>"+
        // 			"</Item></AML>";
    
        // 		tmp1 = inn.applyAML(aml);
    
        	}
        }
    }
    

    string strWriteBackAML=string.Format(@"
    		<AML>
    			<Item type=""In_Meeting_Surveys"" action=""get"" select='id,related_id(in_property,in_expense),source_id' >
    				<source_id>{0}</source_id>
    				<related_id>
    				    <Item type='in_survey' action='get' select=""in_property,id,in_expense"">
    				        <in_write_back>1</in_write_back>
    				    </Item>
    				</related_id>
    
    			</Item>
    		</AML>",meeting_id);
    
    string strResultAML=@"
    		<Item type=""In_Meeting_Surveys_result"" action=""add"">
    			<source_id>{0}</source_id>
    			<in_answer>{1}</in_answer>
    			<related_id>{2}</related_id>
    			<in_participant>muid</in_participant>
    			<in_surveytype>{3}</in_surveytype>
    		</Item>
    		";
    
    string strGetMeetingAML="<AML><Item type='In_Meeting' action='get' id='{#meeting_id}' select='*'/></AML>"
    							.Replace("{#meeting_id}",meeting_id);
    string strMuFilterName;
    bool boolRegOnScene=false;
    itmMeeting=inn.applyAML(strGetMeetingAML);
    strMuFilterName=itmMeeting.getProperty("in_mu_filter");

    //檢查是否有設定in_mu_filter
    if(strMuFilterName==null){
    	itmError=inn.newError("");
    	itmError.setErrorDetail("要求的會議未設定會議使用者規則，請聯絡管理人員。");
    	return itmError;
    }
    //檢查是否仍可報名
    if(itmMeeting.getProperty("in_isfull","0")=="1"){
    	itmError=inn.newError("");
    	itmError.setErrorDetail(itmMeeting.getProperty("in_close_msg","無法報名，原因未設定"));
    	return itmError;
    }
    //檢查是否仍可報名//
    Item itmWriteBacks=inn.applyAML(strWriteBackAML);
    string expense_property_name = "NoExpense"; //有勾選為費用項的問項name 要放一個假欄位,以免後面壞掉
    
    string in_l1_id = "";
    string in_l2_id = "";
    string in_l3_id = "";
    
    int intWriteBackCount=itmWriteBacks.getItemCount();
    //將要回寫的欄位寫入dictionary方便處理。
    for(int q=0;q<intWriteBackCount;q++){
    	Item MeetingSurvey =itmWriteBacks.getItemByIndex(q);
    	Item itmSurvey = MeetingSurvey.getRelatedItem();
    	
    	string survey_id = MeetingSurvey.getProperty("related_id");
    	string in_property = itmSurvey.getProperty("in_property", "");
    	string in_expense = itmSurvey.getProperty("in_expense", "0");
    	
    	dicWriteBacks.Add(survey_id, itmSurvey);
    	
    	if (in_expense =="1")
    	{
    	    expense_property_name = in_property;
    	}
    	if (in_property.Contains("in_l1")) in_l1_id = survey_id;
    	if (in_property.Contains("in_l2")) in_l2_id = survey_id;
    	if (in_property.Contains("in_l3")) in_l3_id = survey_id;
    }
    
    itmMeeting.setProperty("in_l1_id", in_l1_id);
    itmMeeting.setProperty("in_l2_id", in_l2_id);
    itmMeeting.setProperty("in_l3_id", in_l3_id);
    itmMeeting.setProperty("in_expense_property", expense_property_name);
    
    //處理In_Meeting_User的特殊欄位
    	applicant.setProperty("in_mail",this.getProperty("email"));
    	if(this.getProperty("email").Contains("@"))
    	    applicant.setProperty("in_email",this.getProperty("email"));
    	    
   
    //處理[身分證字號(外顯)]
    string in_sno = this.getProperty("email").Split('-')[0];
    if(in_sno.Length  == 10)
    {
        //10碼(身分證處理)
        string in_sno_1 =  in_sno.Substring(0,2);
		string in_sno_2 =  in_sno.Substring(6,4);
		applicant.setProperty("in_sno_display",in_sno_1 + "****" + in_sno_2);//A1****6789
    }
    else if(in_sno.Length  == 18)
    {
        //18碼(身分證處理-大陸)
        string in_sno_1 =  in_sno.Substring(0,6);
		string in_sno_2 =  in_sno.Substring(14,3);
		applicant.setProperty("in_sno_display",in_sno_1 + "********" + in_sno_2 + "X");//110102********888X
    }
    else
    {
        //不屬於前兩者(身分證)可能為學校代碼
        applicant.setProperty("in_sno_display",in_sno);
    }
    
    //建立答案卷並將答案填入

    List<string> staRequiredFields=new List<string>(new string[]{"in_mail"});
    string[] staStuffings=new string[]{meeting_id,null,null,surveytype};//順序 meeting_id,in_answer,In_Survey(id),in_surveytype

    //處理傳過來的答案卷，將要回寫的部分加入到applican的內容中
    // string in_mail = "";
    for(int p=0;p<pms.Length;p++)
    {
        //CCO.Utilities.WriteDebug(strMethodName, "pms:" + pms[p]);
        string[] kvs=pms[p].Split('=');
        string surveyID=kvs[0];
        string[] answers=kvs[1].Split(',');
        for(int e=0;e<answers.Length;e++)
        {
    			staStuffings[1]=answers[e];
    			staStuffings[2]=kvs[0];
                stbAddAnswer.Append(string.Format(strResultAML,staStuffings));	
    	}

    	//將要回寫的部分加入到applican的內容中
    	if(dicWriteBacks.ContainsKey(kvs[0]))
    	{
    		if (kvs[1].Trim() == "Invalid date" || kvs[1].Trim() == "")
    		{
    		    continue;
    		}
    
    		Item WriteBackSurvey = dicWriteBacks[kvs[0]];
    		applicant.setProperty(WriteBackSurvey.getProperty("in_property",""),kvs[1]);
    		
    		string property_name = WriteBackSurvey.getProperty("in_property","");
    		string chk_index = this.getProperty("in_index","");
    		string checkResult="";
            checkResult = checkLimit(CCO, strMethodName, inn, property_name, kvs[1], meeting_id, strMuid, login_resume_current_org , chk_index);        
            if (checkResult !="")
            {
                checkResultMsg += checkResult;
                isAdmission = false;
            }
    	}

       this.setProperty("x" + surveyID,kvs[1]);
    }

    string in_meeting_type = itmMeeting.getProperty("in_meeting_type", "");
    
    
    //活動類型為 game (比賽)、競賽組別不為隊職員、有段位問項才檢查
    bool need_check_degree = false;
    string in_l1 = applicant.getProperty("in_l1", "");
    string new_degree = applicant.getProperty("in_degree", "");
    
    if (in_meeting_type == "game" && in_l1 != "隊職員")
    {
        string sql_degree = @"
            SELECT TOP 1 t2.id, t2.in_property FROM IN_MEETING_SURVEYS t1 WITH(NOLOCK) 
            INNER JOIN IN_SURVEY t2 WITH(NOLOCK) ON t2.id = t1.related_id
            WHERE t1.source_id = '{#meeting_id}'
            AND t2.in_property IN ('in_degree', 'in_degree_label')
        ".Replace("{#meeting_id}", meeting_id);
        
        Item itmDegreeSuvey = inn.applySQL(sql_degree);
        if (!itmDegreeSuvey.isError() && itmDegreeSuvey.getResult() != "")
        {
            //2023-04-19 檢查是不是有隱藏段位
            string sql_degree_isHide = @"
                SELECT top 1 t2.id as survey_id, t2.in_property,t2_event.IN_TARGET,t2_event.IN_ACTION,t2_event.IN_VALUE,t3.IN_PROPERTY
                FROM innovator.IN_MEETING_SURVEYS t1 WITH(NOLOCK) 
                INNER JOIN innovator.IN_SURVEY t2 WITH(NOLOCK) ON t2.id = t1.related_id
                inner join innovator.IN_SURVEY_EVENT t2_event WITH(NOLOCK) ON t2_event.SOURCE_ID = t2.id
                inner join innovator.IN_SURVEY t3 WITH(NOLOCK) ON t2_event.IN_TARGET = t3.id
                WHERE t1.source_id = '{#meeting_id}'
                AND t3.in_property IN ('in_degree', 'in_degree_label') and t2_event.IN_VALUE in ('{#in_l1}')
                and t2_event.IN_ACTION='hide'
            ".Replace("{#meeting_id}", meeting_id).Replace("{#in_l1}", in_l1);
            Item itmDegreeHide = inn.applySQL(sql_degree_isHide);
            if(!itmDegreeHide.isError() && itmDegreeHide.getResult()!=""){
                //有隱藏段位，則不要檢查段位
                need_check_degree = false;
            }else{
                need_check_degree = true;
            }
        }
    
        if (new_degree.Contains("請選擇") || new_degree == "無" || new_degree == "0") new_degree = "";
    
        if (need_check_degree && new_degree == "")
        {
            throw new Exception("選手報名資訊無段位,無法報名");
        }
    }
    
    string sql_temp = "SELECT TOP 1 * FROM IN_RESUME WITH(NOLOCK) WHERE in_sno = '" + in_sno + "'";
    Item itmTempResumt = inn.applySQL(sql_temp);
    if (!itmTempResumt.isError() && itmTempResumt.getResult() != "")
    {
        //lina: 2021.04.22 未啟用帳號暫時處理 _# START:
        if (itmTempResumt.getProperty("in_temp_memo", "") == "1")
        {
            string temp_resume_id = itmTempResumt.getProperty("id", "");
            string prms = "<scene>enable_one_member</scene><resume_id>" + temp_resume_id + "</resume_id>";
            Item result = inn.applyMethod("FIX_CTA_PROMOTIONS", prms);
            
            sql_temp = "UPDATE IN_RESUME SET in_temp_memo = 'enabled' WHERE in_sno = '" + in_sno + "'";
            inn.applySQL(sql_temp);
        }
        //lina: 2021.04.22 未啟用帳號暫時處理 _# END.
        
        //lina: 2022.03.17 段位檢查 _# START:
        if (need_check_degree && in_meeting_type == "game")
        {
            string old_degree = itmTempResumt.getProperty("in_degree", "");
            if (old_degree.Contains("請選擇") || old_degree == "無" || old_degree == "0") old_degree = "";

            if (old_degree == "")
            {
                throw new Exception("系統內選手資訊無段位,請與協會聯絡");
            }
            
            //Resume 有段位資料，報名問項也有段位資料
            if (old_degree != "" && new_degree != "")
            {
                var itmOldDgree = inn.applyMethod("In_Degree_Value", "<in_degree_label>" + old_degree + "</in_degree_label>");
                var itmNewDgree = inn.applyMethod("In_Degree_Value", "<in_degree_label>" + new_degree + "</in_degree_label>");
                
                var old_dgr_code = itmOldDgree.getProperty("in_degree", "");
                var new_dgr_code = itmNewDgree.getProperty("in_degree", "");
                    
                //throw new Exception("Resume 段位資料 = " + old_dgr_code + " vs " + new_dgr_code);
                
                if (new_dgr_code != old_dgr_code)
                {
                    throw new Exception("目前報名資訊與系統內選手資訊的段位不相符,請與協會聯絡");
                }
            }
        }
        //lina: 2022.03.17 段位檢查 _# END.
    }
    
    // sql = "select id from in_meeting_user where source_id='" + meeting_id + "' and in_mail='" + in_mail + "'";
    // Item ExistMU = inn.applySQL(sql);
    // if (ExistMU.getResult()!="")
    // {
    //     throw new Exception("本選手已報名過本項目:" + in_mail);
    // }
    
    string strExpenseProperty = applicant.getProperty(expense_property_name, "");
    string sectionName = GetSectionName(applicant, this.getProperty("register_section_name", ""));


    //協助報名者姓名
    applicant.setProperty("in_creator", this.getProperty("in_creator", ""));
    
    //協助報名者帳號
    string in_creator_sno = applicant.getProperty("in_creator_sno", "");

    if (in_creator_sno == "")
    {    
        in_creator_sno = this.getProperty("current_user_name_sno", "");
        applicant.setProperty("in_creator_sno", in_creator_sno);
    }

    //所屬群組
    string in_group = applicant.getProperty("in_group", "");
    if (in_group == "")
    {   
        in_group = login_resume_group;
        applicant.setProperty("in_group", login_resume_group);
    }
    
    //所屬單位
    string in_current_org = applicant.getProperty("in_current_org", "");
    if (in_current_org == "")
    {   
        in_current_org = login_resume_current_org;
        applicant.setProperty("in_current_org", login_resume_current_org);
    }

    //預設身分均為選手
    applicant.setProperty("in_role","player");

    //處理費用與身分:如果費用欄位 包含斜線,則切下 數字部分,如果包含隊職員三個字,則視為隊職員

    if(strExpenseProperty != "")
    {
        string strExpenseValue = ExpenseValueFromSurveyOption(CCO, strMethodName, inn, itmMeeting, applicant);
        applicant.setProperty("in_gameunit",strExpenseProperty);
        applicant.setProperty("in_expense", strExpenseValue);
    
        if (strExpenseProperty.Contains("隊職員"))
        {
            applicant.setProperty("in_role","stuff");
        }
        else
        {
            applicant.setProperty("in_role","player");
        }
    }
    
      
    applicant.setProperty("in_section_name", sectionName);
    //處理傳過來的答案卷/
    if (isAdmission)
    {
        //正取
        applicant.setProperty("in_note_state", "official");
    }
    else
    {
        //備取
        if (checkResultMsg.Contains("失敗"))
        {
            itmError = inn.newError("");
            itmError.setErrorDetail("<div style='position: relative;'>["+applicant.getProperty("in_name", "")+"]"+ checkResultMsg + "</div>");
            return itmError;
        }
        else
        {
            applicant.setProperty("in_note_state", "waiting");
        }
    }
    
    //判斷輸入的時間是否在區間內
    string check_birth = applicant.getProperty("in_birth","no_data");
    if(check_birth != "no_data")
    {
        DateTime check_date = Convert.ToDateTime(check_birth);
    	DateTime date_range_s = Convert.ToDateTime("1/1/1753 12:00:00 AM");
    	DateTime date_range_e = Convert.ToDateTime("12/31/9999 11:59:59 PM");
        if(check_date.CompareTo(date_range_s)<0 || check_date.CompareTo(date_range_e)>0)
        {   
            Item itmErr2=inn.newError("日期格式錯誤!請重新輸入!");
            itmErr2.setErrorDetail("日期格式錯誤!請重新輸入!");
        	return itmErr2;
        }
    }
    
    if (is_gym_registry)
    {
        //單位註冊
    }
    else if(applicant.getProperty("in_l1","") != "隊職員")
    {
        //非隊職員(身分證+生日不可為空)
        if(applicant.getProperty("in_sno","") == "")
        {
            Item itmErr2=inn.newError("請輸入身份證字號!");
            itmErr2.setErrorDetail("請輸入身份證字號!");
        	return itmErr2;
        }

        string in_org_value = in_org != ""
            ? in_org
            : applicant.getProperty("in_org","");
       
        if(in_org_value != "1" && applicant.getProperty("in_birth","") == "")
        {
            Item itmErr2=inn.newError("請輸入出生年月日!");
            itmErr2.setErrorDetail("請輸入出生年月日!");
        	return itmErr2;
        }
    }
    
    if (isUserId != "")
    {
        Item CurrentUser = inn.getItemById("User", isUserId);
        //用偷藏的ID取得當前登入者
        applicant.setProperty("in_creator", CurrentUser.getProperty("last_name", ""));;//協助報名者
        applicant.setProperty("in_creator_sno", CurrentUser.getProperty("login_name", ""));//協助報名者帳號
        in_creator_sno = CurrentUser.getProperty("login_name", "");
    }
    
    //補被報名者修改不改協助報名者
    if(this.getProperty("isSingleUser","") == "1" || isUserId =="")
    {
        applicant.setProperty("in_creator", this.getProperty("su_creator", ""));;//協助報名者
        applicant.setProperty("in_creator_sno", this.getProperty("su_creator_sno", ""));//協助報名者帳號
        in_creator_sno = this.getProperty("su_creator", "");
    }

    //檢查是否為現場報名
    string strCheckMeetingFunctionAML=@"<AML>
    									<Item type='In_Meeting_Functiontime' action='get' select='id,in_type,in_date_s,in_date_e'>
    									    <in_action>2</in_action>
    										<source_id>{#meeting_id}</source_id>
    										<in_date_s condition='lt'>{#today}</in_date_s>
    										<in_date_e condition='gt'>{#today}</in_date_e>
    									</Item>
    								</AML>"
    								.Replace("{#meeting_id}",meeting_id)
    								.Replace("{#today}",System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

    Item itmSignOnScene=inn.applyAML(strCheckMeetingFunctionAML);
    
    if(!itmSignOnScene.isError() && !itmSignOnScene.isEmpty())
    {
        boolRegOnScene=true;
    }


    CheckGymClosed(CCO, strMethodName, inn, meeting_id, in_creator_sno, "進行報名");
    
    if(boolIsModify)
    {
        //lina 2021.02.18 將審核狀態清空
        applicant.setProperty("in_verify_result", "");
        applicant.setProperty("in_ass_ver_result", "");
                
        applicant.setAttribute("where","[In_Meeting_User].id='"+strMuid+"'");
        mUser=applicant.apply("merge");
	}
    else
    {
        string _in_group = applicant.getProperty("in_group", "");
        string _in_current_org = applicant.getProperty("in_current_org", "");
        
        if (_in_current_org == null || _in_current_org == "")
        {
            applicant.setProperty("in_current_org", _in_group);
        }
        
        applicant.setProperty("in_regdate",System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

		applicant = applicant.apply(strMuFilterName);

        string _in_index = applicant.getProperty("in_index","");
		//如果有 in_index 則沿用,否則就給號
		if(_in_index == "")
		{
			Item itmIndex = GetLevelMaxIndex(CCO, strMethodName, inn, meeting_id, number_keys, applicant);
			
			if(itmIndex.getResult()=="")
			{
				applicant.setProperty("in_index", "00001");
			}
			else
			{
				int intIndex = int.Parse(itmIndex.getProperty("c1","1")) + 1;
				applicant.setProperty("in_index", intIndex.ToString("00000"));
			}
		}
		
		//lina 2021.03.05-03.16
		Item itmMethodData = inn.newItem("In_Meeting_User");
		itmMethodData.setProperty("in_committee", applicant.getProperty("in_committee", ""));
		itmMethodData.setProperty("in_degree", applicant.getProperty("in_degree", ""));
		itmMethodData.setProperty("in_degree_label", applicant.getProperty("in_degree_label", ""));
		
        //段位數值與標籤處理
		Item itmDegreeRslt = itmMethodData.apply("In_Degree_Value");
		applicant.setProperty("in_degree", itmDegreeRslt.getProperty("in_degree", ""));        
		      
		if (is_gym_registry)
		{
    		//委員會處理
    		Item itmCmtRslt = itmMethodData.apply("In_Committee_Value");
    		applicant.setProperty("in_manager_name", itmCmtRslt.getProperty("in_manager_name", ""));  
    		applicant.setProperty("in_manager_org", itmCmtRslt.getProperty("in_manager_org", "")); 
    		applicant.setProperty("in_manager_area", itmCmtRslt.getProperty("in_manager_area", "")); 
		}
		
		if (applicant.getProperty("in_sno","") == "")
        {
            //代表這是隊職員 (教練可能用實名制)
            applicant.setProperty("in_mail", DateTime.Now.ToString("MMddHHmmssfff") + "-" + applicant.getProperty("in_l1",""));
        }
        
        //特殊報名規則
        //--站台
        Item itmSite = inn.applyAML("<AML><Item type='In_Site' action='get' select='*'/></AML>");
        string st_reg_validate_id = itmSite.getProperty("in_reg_validate_1","");
        if (st_reg_validate_id != "")
        {
            string st_reg_validate_name = itmSite.getPropertyAttribute("in_reg_validate_1","keyed_name","");
            applicant.apply(st_reg_validate_name);
        }
        //--賽事
        string mt_reg_validate_id = itmMeeting.getProperty("in_reg_validate_method","");
        if (mt_reg_validate_id != "")
        {
            string mt_reg_validate_name = itmMeeting.getPropertyAttribute("in_reg_validate_method","keyed_name","");
            applicant.apply(mt_reg_validate_name);
        }
        //特殊報名規則
        
        //新增與會者
        CCO.Utilities.WriteDebug(strMethodName, "新增與會者 sid: " + applicant.getProperty("in_sno", ""));
		mUser = applicant.apply("add");
	}

    muid = mUser.getID();

    stbAddAnswer.Replace("muid",muid);

    //處理照片
	if (this.getProperty("photo","") != "")
	{
	    aml = "<AML>"+
	        "<Item type='In_Variable_Detail' action='get'>"+
	        "<source_id>D4073B9D20CC4063A7D5F9CF9B4B21B3</source_id>"+
	        "<in_name>head_photo</in_name>"+
	        "</Item></AML>";
	    Item HeadPhoto = inn.applyAML(aml);
        string head_folder = HeadPhoto.getProperty("in_value","");

		string fileNameWitPath =head_folder + @"\" + mUser.getProperty("in_sno") + ".png";
		using (FileStream fs = new FileStream(fileNameWitPath, FileMode.Create))
		{
			using (BinaryWriter bw = new BinaryWriter(fs))

			{
			    string photo_base64 = this.getProperty("photo","");
			    photo_base64 = photo_base64.Replace("data:image/png;base64,","");
				byte[] data = Convert.FromBase64String(photo_base64);
				bw.Write(data);
				bw.Close();
			}
		}

		sql = "Update In_Meeting_User set in_photo='" +  mUser.getProperty("in_sno") + ".png' where id='" + mUser.getID() + "'";

		inn.applySQL(sql);

		stbAddAnswer.Replace("p.png", mUser.getProperty("in_sno") + ".png");
	}

    //處理照片
	inn.applyAML("<AML>"+stbAddAnswer.ToString()+"</AML>");//寫入答案卷答案
	itmMeeting.apply("In_Update_MeetingOnMUEdit");
    mUser.apply("In_Merge_ThisResume");
    if(!boolIsModify &&boolRegOnScene &&  mUser.getProperty("in_note_state","")!="official" && itmMeeting.getProperty("in_register_url","")=="MeetingRegistryContinous.html")
    {
		mUser.setProperty("inn_message","使用者報名狀態為備取，請洽服務台。&"+muid);
		mUser.setProperty("inn_reg_on_scene","true");
	}

    if(this.getProperty("in_note_state","")=="")
    {
        this.getProperty("in_note_state",applicant.getProperty("in_note_state",""));  //因為有時候是從手工報名過來的,所以要做切換
    }

    this.setProperty("in_note_state",this.getProperty("in_note_state"));
    this.setProperty("in_sno",mUser.getProperty("in_sno"));
    this.setProperty("in_email",mUser.getProperty("in_email"));
    this.setProperty("in_tel",mUser.getProperty("in_tel"));

    if(this.getProperty("muid","")=="")
    {
        this.setProperty("muid",muid);
    }
    
    Item itmToUserResult;

    if(mUser.getProperty("in_sno","")!="")
    {
        //有身分證號才產生真人 代表不是隊職員
        if(surveytype=="1a")
        {
             mUser.setProperty("login_name",mUser.getProperty("in_sno"));
            itmToUserResult = mUser.apply("in_merge_single_resume");
        }
        else
        {
            sql = "SELECT TOP 1 * FROM IN_RESUME WITH(NOLOCK) WHERE in_sno = '" + mUser.getProperty("in_sno","") + "'";
            Item itm_resume = inn.applySQL(sql);
            
            //lina 2021.01.22 新增禁賽卡控
            CheckBanned(CCO, strMethodName, inn, mUser, itm_resume);
            
            //2020.07.29 出生年月日如果為空值轉時間格式會出錯
            string in_birth = mUser.getProperty("in_birth","");
            if (in_birth == "")
            {
                in_birth = "1900-01-01T00:00:00";
            }
            string mUser_birth = DateTime.Parse(in_birth).ToString("yyyy/MM/dd");
            string[] Special_text = new string[]{"?","=",".","*","[","@","#","$","%","^","&",".","+","-","]"," ","/",@"\"};//特殊字元

            string mUse_current_org = mUser.getProperty("in_current_org","");
        
            if(itm_resume.getResult() != "")
            {
                string itm_resumer_birth = "";
                string resume_birth = itm_resume.getProperty("in_birth","");
                if (in_birth == "")
                {
                    itm_resumer_birth = DateTime.Parse("1900-01-01T00:00:00").ToString("yyyy/MM/dd");
                }
                else
                {
                    itm_resumer_birth = DateTime.Parse(in_birth).AddHours(8).ToString("yyyy/MM/dd");
                }
            
                
                if (mUser_birth != itm_resumer_birth 
                    || mUser.getProperty("in_gender","") != itm_resume.getProperty("in_gender","") 
                    || mUser.getProperty("in_name","") != itm_resume.getProperty("in_name",""))
                {
                    //前台傳入值1代表可新增in_meeting_user
                    if (OnlyInsMUser !="1")
                    {
                        throw new Exception("目前報名資訊與系統內選手資訊不相符,請至系統設定的[帳號設定]修改");
                    }
                }
                else
                {
                    //因前台檢查資訊為多筆，新增為單筆，如單筆資訊相同則將狀態改為0
                    OnlyInsMUser = "0";
                }
            }
            else
            {
                OnlyInsMUser = "0";
            }
        
            //檢查是否有特殊字元存在
            foreach(string _text in Special_text)
            {
                if(mUse_current_org.Contains(_text))
                {
                    throw new Exception("所屬單位包含特殊字元");
                }
            }
            
            if (OnlyInsMUser !="1")
            {
                itmToUserResult = mUser.apply("In_Meeting_ToFormal1User");
                if(itmToUserResult.getResult().Contains("發生錯誤"))
                {
                    string[] RegErrorMsg = itmToUserResult.getResult().Split(':');
                    if(RegErrorMsg.Length>1)
                    {
                        throw  new Exception(RegErrorMsg[1]);
                    }
                    else
                    {
                        throw  new Exception(itmToUserResult.getResult());
                    }
                }
            }
        }
    
        //CCO.Utilities.WriteDebug(strMethodName, "itmToUserResult:" + itmToUserResult.dom.InnerXml);
        Item itmmeeting_account = inn.getItemByKeyedName("In_Variable","in_meeting_account_verify");
        string meeting_account_verify =  itmmeeting_account.getProperty("in_value","");//從設定參數以依據名稱取值
        CCO.Utilities.WriteDebug(strMethodName, meeting_account_verify);
    
        switch(meeting_account_verify) 
        {
            //自動審核
            case "auto_verify":
                //ToDo: 
                //走到這裡,一定可以透過身分證號去抓對應的 in_resume, 抓到對應的 in_resume之後,再取其中的密碼 in_password_plain 欄位,送到前端去(目前 dll 只認得 in_info_1,in_info_2,in_info_3,in_info_4,in_info_5)
                //此功能需搭配我新製作的 \\192.168.0.81\c$\site\iact\bin\InnoAppWeb.dll (2.0.19.1028版本)
                string son = mUser.getProperty("in_sno","");
                aml = "<AML>"+
                    "<Item type='In_Resume' action='get'>"+
                    "<login_name>" + son + "</login_name>"+
                    "</Item></AML>";
                Item Resume = inn.applyAML(aml);
           
                //CCO.Utilities.WriteDebug("123", mUser.getProperty("in_sno",""));
                //CCO.Utilities.WriteDebug("123", mUser.dom.InnerXml);
                //CCO.Utilities.WriteDebug("123", Resume.getProperty("in_password_plain",""));
                //mUser.setProperty("in_info_1", Resume.getProperty("in_password_plain",""));  //傳給前端密碼
                string in_tel = mUser.getProperty("in_tel","").Replace("-","");
                //傳給前端密碼(提示訊息加上手機後4碼)
                if (in_tel.Length == 10 && in_tel.StartsWith("09"))
                {
                    mUser.setProperty("in_info_1","<br>手機後4碼&emsp;"+ Resume.getProperty("in_password_plain",""));   
                    mUser.setProperty("in_info_2", Resume.getProperty("in_password_plain",""));   
                }
                else
                {
                    mUser.setProperty("in_info_1", Resume.getProperty("in_password_plain",""));
                    mUser.setProperty("in_info_2", Resume.getProperty("in_password_plain",""));
                }
                
                //CCO.Utilities.WriteDebug("123", System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

                sql = "Update In_Meeting_User set in_confirm_mail='" + System.DateTime.Now.AddHours(-8).ToString("yyyy-MM-ddTHH:mm:ss") + "' where id='" + mUser.getID() + "'";//將當前時間更新到通知時間
    	        inn.applySQL(sql);
    	        //自動審核走這邊(給狀態validated,呼叫寄信的method(In_Send_MeetingMessage))
                string Notify_Type = this.getProperty("notify_type","validated"); 
                mUser.setProperty("Notify_Type",Notify_Type);
                mUser.apply("In_Send_MeetingMessage");
                break;
            //人工審核
            case "manual_verify":
                mUser.apply("In_Send_MeetingConfirm");
                break;
                //如果無此參數預設人工審核
            default:
                mUser.apply("In_Send_MeetingConfirm");
                
            break;    
        }
    }

    sql = "SELECT top 1 * FROM In_Resume WITH(NOLOCK) where in_sno = N'" + mUser.getProperty("in_sno","") +"'";
    Item mUserResume = inn.applySQL(sql);
    
    if (mUserResume.getResult() != "")
    {
        string muser_add = mUser.getProperty("in_add","");
        string resume_photo = mUserResume.getProperty("in_photo","");
        string resume_user_id = mUserResume.getProperty("in_user_id","");
        
        sql = "UPDATE IN_MEETING_USER SET"
            + " in_photo1 = '" + resume_photo + "'"
            + ", in_add = N'" + muser_add + "'"
            + ", related_id = '" + resume_user_id + "'"
            + " WHERE id = '" + muid + "'";
            
        inn.applySQL(sql);//少補一刀 沒執行到 所以地址沒更新
        
        //lina 2021.01.06 建立成員關係
        if (login_resume_group =="中華跆協")
        {
            if(this.getProperty("su_creator_sno", "") !="M001" && this.getProperty("su_creator_sno", "") !="")
            {
                sql = "SELECT * FROM in_resume WHERE in_sno ='" + this.getProperty("su_creator_sno", "") + "'";
                Item itmRe = inn.applySQL(sql);
                if (!itmRe.isError()){
                    login_resume_id  = itmRe.getProperty("id","");
                    BindResumeRelation(CCO, strMethodName, inn, login_resume_id, mUser, mUserResume);
                }
            }
        }
        else
        {
            BindResumeRelation(CCO, strMethodName, inn, login_resume_id, mUser, mUserResume);
        }
     
        // BindResumeRelation(CCO, strMethodName, inn, login_resume_id, mUser, mUserResume);
    }
    
    string current_sno = applicant.getProperty("in_sno", "");
    string in_manager_org = applicant.getProperty("in_manager_org", "");
    string in_manager_name = applicant.getProperty("in_manager_name", "");
    string in_manager_area = applicant.getProperty("in_manager_area", "");
    
    if (applicant.getProperty("in_member_apply", "") == "否")
    {
        if (is_gym_registry)
        {
            sql = "UPDATE IN_RESUME SET"
                + " in_member_status = N'暫時會員'"
                + ", in_member_role = N'sys_9999'"
                + ", in_manager_org = '" + in_manager_org + "'"
                + ", in_manager_name = N'" + in_manager_name + "'"
                + ", in_manager_area = N'" + in_manager_area + "'"
                + " WHERE in_sno = '" + current_sno + "'";
        }
        else
        {
           sql = "UPDATE IN_RESUME SET in_member_status = N'暫時會員', in_member_role = N'sys_9999' WHERE in_sno = '" + current_sno + "'";
        }
        
        inn.applySQL(sql);
    }
    else if (applicant.getProperty("in_member_apply", "") == "是")
    {
        if (is_gym_registry)
        {
            sql = "UPDATE IN_RESUME SET"
                + " in_member_status = N'暫時會員'"
                + ", in_member_role = N'sys_9999'"
                + ", in_manager_org = '" + in_manager_org + "'"
                + ", in_manager_name = N'" + in_manager_name + "'"
                + ", in_manager_area = N'" + in_manager_area + "'"
                + " WHERE in_sno = '" + current_sno + "'";
        }
        else
        {
           sql = "UPDATE IN_RESUME SET in_member_status = N'暫時會員', in_member_role = N'sys_9999' WHERE in_sno = '" + current_sno + "'";
        }
        
        inn.applySQL(sql);
        
        Item itmData = inn.newItem();
        itmData.setType("In_Resume");
        itmData.setProperty("in_sno", current_sno);
        itmData.setProperty("inn_new_apply", "1");
        itmData.apply("In_Payment_Resume");
    }
    
    if (checkResultMsg != "")
    {
        mUser.setProperty("in_info_5", "<div style='position: relative;'>["+mUser.getProperty("in_name", "")+"]"+ checkResultMsg+"</div>");
    }
    
    return mUser;
}

private string GetNoLevelValues(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string meeting_id)
{
    string result = "";
    
    //預設為階層一
    string default_valie = "in_l1";

    string sql = @"
SELECT 
    t1.in_property,
    t1.in_is_nokey
FROM 
    IN_SURVEY t1 WITH(NOLOCK)
INNER JOIN 
    IN_MEETING_SURVEYS t2 WITH(NOLOCK) ON t2.related_id = t1.id 
WHERE 
    t2.SOURCE_ID = N'{#meeting_id}' 
    AND t1.IN_CLIENT_REMOVE = N'0'
    AND t1.IN_PROPERTY IN (N'in_l1', N'in_l2', N'in_l3')
ORDER BY 
    t1.IN_PROPERTY
";
    sql = sql.Replace("{#meeting_id}", meeting_id);

    //CCO.Utilities.WriteDebug(strMethodName, "sql_levels: " + sql);

    Item items = inn.applySQL(sql);
    
    if (items.isError())
    {
        return default_valie;
    }
    
    int count = items.getItemCount();
    if (count == 0)
    {
        return default_valie;
    }
    
    string number_key_level = "";
    for (int i = 0; i < count; i++)
    {
        Item item = items.getItemByIndex(i);
        string in_property = item.getProperty("in_property", "").Trim();
        string in_is_nokey = item.getProperty("in_is_nokey", "").Trim();
        //CCO.Utilities.WriteDebug(strMethodName, "in_property: " + in_property + ", in_is_nokey: " + in_is_nokey);

        if (in_is_nokey == "1")
        {
            if (number_key_level != "")
            {
                number_key_level += ",";
            }
            number_key_level += in_property;
        }
    }

    //CCO.Utilities.WriteDebug(strMethodName, "number_key_level: " + number_key_level);

    if (number_key_level == "" || number_key_level == default_valie)
    {
        result = default_valie;
    }
    else
    {
        result = number_key_level;
    }
    return result;
}

//取得同組成員
private Item GetRegistedMeetingUsers(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string meeting_id, string number_keys, string strMuid)
{
    string sql = @"
SELECT
    in_creator_sno,
    in_group,
    in_current_org,
    in_l1, 
    in_l2, 
    in_l3, 
    in_l4, 
    in_index 
FROM 
    IN_MEETING_USER WITH(NOLOCK)
WHERE
    id = N'{#meeting_user_id}'
";

    sql = sql.Replace("{#meeting_user_id}", strMuid);
    Item itmFirstUser = inn.applySQL(sql);
    
    string in_index = itmFirstUser.getProperty("in_index", "");
    string in_l1 = itmFirstUser.getProperty("in_l1", "").Trim();
    string in_l2 = itmFirstUser.getProperty("in_l2", "").Trim();
    string in_l3 = itmFirstUser.getProperty("in_l3", "").Trim();
    string in_l4 = itmFirstUser.getProperty("in_l4", "").Trim();

    sql = "SELECT id, in_creator_sno FROM In_Meeting_User WITH(NOLOCK) WHERE source_id = '" + meeting_id + "'";

    if (number_keys.Contains("in_l1") && in_l1 != "")
    {
         sql += " AND in_l1 = N'" + in_l1 + "'";
    }
   
    if (number_keys.Contains("in_l2") && in_l2 != "")
    {
         sql += " AND in_l2 = N'" + in_l2 + "'";
    }
    
    if (number_keys.Contains("in_l3") && in_l3 != "")
    {
         sql += " AND in_l3 = N'" + in_l3 + "'";
    }
    
    if (number_keys.Contains("in_l4") && in_l4 != "")
    {
         sql += " AND in_l4 = N'" + in_l4 + "'";
    }

    sql += " AND in_index = N'" + in_index + "'";
    
    //CCO.Utilities.WriteDebug(strMethodName, "sql_users: " + sql);

    return	inn.applySQL(sql);
}

//取得該組當前最大序號
private Item GetLevelMaxIndex(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string meeting_id, string number_keys, Item applicant)
{
    string sql = "";
    
    string in_l1 = applicant.getProperty("in_l1", "");
    string in_l2 = applicant.getProperty("in_l2", "");
    string in_l3 = applicant.getProperty("in_l3", "");
    string in_l4 = applicant.getProperty("in_l4", "");

    sql = "SELECT max(in_index) as c1 FROM [In_Meeting_User] WITH(NOLOCK) WHERE source_id = N'" + meeting_id + "'";

    if (number_keys.Contains("in_l1"))
    {
         sql += " AND in_l1 = N'" + in_l1 + "'";
    }
   
    if (number_keys.Contains("in_l2"))
    {
         sql += " AND in_l2 = N'" + in_l2 + "'";
    }
    
    if (number_keys.Contains("in_l3"))
    {
         sql += " AND in_l3 = N'" + in_l3 + "'";
    }
    
    if (number_keys.Contains("in_l4") && in_l4 != "")
    {
         sql += " AND in_l4 = N'" + in_l4 + "'";
    }

    //CCO.Utilities.WriteDebug(strMethodName, "sql_index: " + sql);
    
    return	inn.applySQL(sql);
}

private string ExpenseValueFromSurveyOption(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, Item itmMeeting, Item applicant)
{
    string in_property = itmMeeting.getProperty("in_expense_property", "");

    Item items = null;

    if (in_property.Contains("in_l3")) 
    {
        items = GetOptions123(CCO, strMethodName, inn, itmMeeting, applicant);
    }
    else if (in_property.Contains("in_l2")) 
    {
        items = GetOptions12(CCO, strMethodName, inn, itmMeeting, applicant);
    }
    else if (in_property.Contains("in_l1")) 
    {
        items = GetOptions1(CCO, strMethodName, inn, itmMeeting, applicant);
    }
    
    if (items == null || items.isError())
    {
        CCO.Utilities.WriteDebug(strMethodName, "費用異常: " + in_property + " _# muser" + applicant.dom.InnerXml);
        throw new Exception("費用異常");
    }
    else if (items.getResult() != "")
    {
        return items.getResult();
    }
    else if (items.getItemCount() > 1)
    {
        CCO.Utilities.WriteDebug(strMethodName, "出現多筆費用: " + in_property + " _# muser" + applicant.dom.InnerXml);
        throw new Exception("出現多筆費用");
    }
    else 
    {
        throw new Exception("查無費用資訊");
    }
}

private Item GetOptions1(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, Item itmMeeting, Item applicant)
{
    string in_l1_id = itmMeeting.getProperty("in_l1_id", "");
    string in_l1_val = applicant.getProperty("in_l1", "");

    string sql = @"
SELECT
    TOP 1 t1.in_expense_value
FROM 
    IN_SURVEY_OPTION t1 WITH(NOLOCK)
WHERE
    t1.SOURCE_ID = '{#in_l1_id}'
    AND t1.in_value = N'{#in_l1_val}'
";
    sql = sql.Replace("{#in_l1_id}", in_l1_id)
        .Replace("{#in_l1_val}", in_l1_val);

    //CCO.Utilities.WriteDebug(strMethodName, "(lv1) sql: " + sql);

    return inn.applySQL(sql);
}

private Item GetOptions12(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, Item itmMeeting, Item applicant)
{
    string in_l1_id = itmMeeting.getProperty("in_l1_id", "");
    string in_l2_id = itmMeeting.getProperty("in_l2_id", "");
    string in_l1_val = applicant.getProperty("in_l1", "");
    string in_l2_val = applicant.getProperty("in_l2", "");

    string sql = @"
SELECT
    TOP 1 t2.in_expense_value
FROM 
    IN_SURVEY_OPTION t1 WITH(NOLOCK)
INNER JOIN (
        SELECT IN_FILTER, IN_VALUE, IN_LABEL, SORT_ORDER, IN_EXPENSE_VALUE, IN_EXTEND_VALUE FROM IN_SURVEY_OPTION WITH(NOLOCK) WHERE SOURCE_ID = '{#in_l2_id}'
    ) t2 ON t2.IN_FILTER = t1.IN_VALUE
WHERE
    t1.SOURCE_ID = '{#in_l1_id}'
    AND t1.in_value = N'{#in_l1_val}'
    AND t2.in_value = N'{#in_l2_val}'
";
    sql = sql.Replace("{#in_l1_id}", in_l1_id)
        .Replace("{#in_l2_id}", in_l2_id)
        .Replace("{#in_l1_val}", in_l1_val)
        .Replace("{#in_l2_val}", in_l2_val);

    //CCO.Utilities.WriteDebug(strMethodName, "(lv2) sql: " + sql);
        
    return inn.applySQL(sql);
}

private Item GetOptions123(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, Item itmMeeting, Item applicant)
{
    string in_l1_id = itmMeeting.getProperty("in_l1_id", "");
    string in_l2_id = itmMeeting.getProperty("in_l2_id", "");
    string in_l3_id = itmMeeting.getProperty("in_l3_id", "");
    string in_l1_val = applicant.getProperty("in_l1", "");
    string in_l2_val = applicant.getProperty("in_l2", "");
    string in_l3_val = applicant.getProperty("in_l3", "");

    string sql = @"
SELECT
    TOP 1 t3.in_expense_value
FROM 
    IN_SURVEY_OPTION t1 WITH(NOLOCK)
INNER JOIN (
        SELECT IN_FILTER, IN_VALUE, IN_LABEL, SORT_ORDER, IN_EXPENSE_VALUE, IN_EXTEND_VALUE FROM IN_SURVEY_OPTION WITH(NOLOCK) WHERE SOURCE_ID = '{#in_l2_id}'
    ) t2 ON t2.IN_FILTER = t1.IN_VALUE
INNER JOIN (
        SELECT IN_FILTER, IN_VALUE, IN_LABEL, SORT_ORDER, IN_EXPENSE_VALUE, IN_EXTEND_VALUE FROM IN_SURVEY_OPTION WITH(NOLOCK) WHERE SOURCE_ID = '{#in_l3_id}'
    ) t3 ON t3.IN_FILTER = t2.IN_VALUE
WHERE
    t1.SOURCE_ID = '{#in_l1_id}'
    AND t1.in_value = N'{#in_l1_val}'
    AND t2.in_value = N'{#in_l2_val}'
    AND t3.in_value = N'{#in_l3_val}'
";
    sql = sql.Replace("{#in_l1_id}", in_l1_id)
        .Replace("{#in_l2_id}", in_l2_id)
        .Replace("{#in_l3_id}", in_l3_id)
        .Replace("{#in_l1_val}", in_l1_val)
        .Replace("{#in_l2_val}", in_l2_val)
        .Replace("{#in_l3_val}", in_l3_val);

    //CCO.Utilities.WriteDebug(strMethodName, "(lv3) sql: " + sql);
        
    return inn.applySQL(sql);
}

private string GetExpenseValue(string contents)
{
    if (contents == null || contents == "") 
    {
        return "";
    }
    
	string value = contents.Replace("元", "");
	double result;
	if (double.TryParse(value, out result))
	{
		return result.ToString();
	}
	else
	{
		return "";
	}
}

//組合總名稱(in_section_name)
private string GetSectionName(Item applicant, string register_section_name)
{
    if (register_section_name != "") 
    {
        return register_section_name;   
    }
    else 
    {
        string result = "";
        for (int j = 1; j <= 4; j++)
        {
            string s = applicant.getProperty("in_l" + j,"");
            if(s != "")
            {
                string[] arr = s.Split('/');
                if (arr != null && arr.Length > 0)
                {
                    result += arr[0] + "-";
                }
                else 
                {
                    result += s + "-";
                }
            }
        }
        result = result.Trim('-');
    
        return result;
    }
}

private bool IS_CHECKED_GYM_STATUS = false;
private bool IS_GYM_CLOSED = true;
private string GYM_CLOSED_MESSAGE = "已完成報名，無法再異動報名資料";

//檢查所屬群組是否已關閉，如果是，不允許異動報名資料
private void CheckGymClosed(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string meeting_id, string in_creator_sno, string memo)
{
    if (IS_CHECKED_GYM_STATUS && IS_GYM_CLOSED)
    {
        throw new Exception(GYM_CLOSED_MESSAGE);
    }
    else 
    {
        string sql = "SELECT in_creator_sno FROM In_Meeting_GymList WITH(NOLOCK) WHERE source_id = '{#meeting_id}' AND in_creator_sno = N'{#in_creator_sno}'"
            .Replace("{#meeting_id}", meeting_id)
            .Replace("{#in_creator_sno}", in_creator_sno);
        
        //CCO.Utilities.WriteDebug(strMethodName, "[" + memo + "]" + "sql: " + sql);
    
        bool isClosed = true;
        Item itmClosed = inn.applySQL(sql);
        if (itmClosed.isError() || itmClosed.getResult() == "")
        {
            isClosed = false;
        }
        
        IS_GYM_CLOSED = isClosed;
        IS_CHECKED_GYM_STATUS = true;
        
        if (isClosed)
        {
            //CCO.Utilities.WriteDebug(strMethodName, "【"+ memo + "】單位已完成報名，sql: " + sql);
            throw new Exception(GYM_CLOSED_MESSAGE);
        }
    }
}

// private string GetResumeCurrentOrg(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string in_creator_sno)
// {
//     if (in_creator_sno == null || in_creator_sno == "")
//     {
//         return "";
//     }
    
//     string sql = "SELECT TOP 1 in_current_org FROM IN_RESUME WITH(NOLOCK) WHERE in_sno = N'" + in_creator_sno + "'";
//     Item item = inn.applySQL(sql);
//     if (item.isError() || item.getItemCount() <= 0)
//     {
//         return "";
//     }
//     else
//     {
//         return item.getProperty("in_current_org", "");
//     }
// }

//建立成員關係
private void BindResumeRelation(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string login_resume_id, Item mUser, Item mUserResume)
{
    string in_sno = mUser.getProperty("in_sno", "");
    string in_l1 = mUser.getProperty("in_l1", "");
    string in_l2 = mUser.getProperty("in_l2", "");
    
    //CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# start: " + in_sno);
    
    if (login_resume_id == "")
    {
        CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# 登入者 resume id 無資料");
        return;
    }
    
    
    string in_user_resume = mUserResume.getProperty("id", "");
    
    string in_resume_role = "reg_110"; //選手
    string in_resume_remark = "選手";
    
    if (in_sno == "")
    {
        CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# Player 身分證號 無資料");
        return;
    }

    if (in_l1 == "隊職員" && in_l2 != "")
    {
        in_resume_remark = in_l2;
        switch(in_l2)
        {
            case "領隊":
                in_resume_role = "reg_210";
                break;
            case "管理":
                in_resume_role = "reg_220";
                break;
            case "教練":
                in_resume_role = "reg_230";
                break;
        }
    }
    
    string sql = "SELECT id FROM In_Resume_Resume WITH(NOLOCK) WHERE source_id = '" + login_resume_id +"' AND related_id = '" + in_user_resume + "'";
    //CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# sql: " + sql);
    Item itmQuery = inn.applySQL(sql);
    
    if (itmQuery.isError() )
    {
        CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# sql 發生錯誤");
        return;
    }
    
    if (itmQuery.getResult() != "")
    {
        //CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# 已存在，不處理");
        return;
    }
    
    Item itmResumeRole = inn.newItem("In_Resume_Resume", "add");
    itmResumeRole.setProperty("source_id", login_resume_id);
    itmResumeRole.setProperty("related_id", in_user_resume);
    itmResumeRole.setProperty("in_resume_role", in_resume_role);
    itmResumeRole.setProperty("in_resume_remark", in_resume_remark);
    itmResumeRole = itmResumeRole.apply();
    
    //CCO.Utilities.WriteDebug(strMethodName, "建立成員關係 _# end: 建立成功");
}

//禁賽卡控
private void CheckBanned(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, Item itmMUser,  Item itmResume)
{
    string in_l1 = itmMUser.getProperty("in_l1", "");
    if (in_l1 == "隊職員")
    {
        //隊職員不卡控
        return;
    }
    
    if (itmResume.isError() || itmResume.getResult() == "")
    {
        return;
    }
    
    string resume_id = itmResume.getProperty("id", "");
    string in_name = itmResume.getProperty("in_name", "");
    
    string sql = @"
SELECT 
	*
FROM 
	IN_RESUME_BANNED WITH(NOLOCK) 
WHERE 
	source_id = '{#resume_id}'
	AND GETDATE() BETWEEN DATEADD(hour, 8, in_date_s) AND DATEADD(hour, 8, in_date_e)
";

    sql = sql.Replace("{#resume_id}", resume_id);
    
    Item itmRBanneds = inn.applySQL(sql);
    
    if (itmRBanneds.isError())
    {
        
    }
    else if (itmRBanneds.getItemCount() > 0)
    {
        throw new Exception("選手 " + in_name + " 禁賽中");
    }
}
private string checkLimit(Aras.Server.Core.CallContext CCO, string strMethodName, Innovator inn, string in_property, string in_label, string meeting_id , string strMuid, string in_current_org, string chk_index)
        {
            string question ="";
            string sql = @" SELECT * FROM in_meeting_user  WITH(NOLOCK) 
                            WHERE SOURCE_ID='{#meeting_id}' AND id = '{#strMuid}' AND {#in_property} ='{#in_label}' ";
            sql = sql.Replace("{#in_property}", in_property)
                    .Replace("{#meeting_id}", meeting_id)
                    .Replace("{#in_label}", in_label)
                    .Replace("{#strMuid}", strMuid);
            
            Item itmUser = inn.applySQL(sql);

            
            //修改時判斷是否值無改變，沒改變則不需驗證人數
            if (itmUser.getItemCount() > 0 && !itmUser.isError())
            {
                return "";
            }
            
            //判斷單位限制人數
             sql = @" SELECT t2.in_property, IN_LABEL, IN_QUESTIONS, reg_curr_qty, in_limit_current
                    FROM innovator.IN_MEETING_SURVEYS AS T1 WITH(NOLOCK)
                    JOIN  innovator.IN_SURVEY AS T2 WITH(NOLOCK)
                    ON T1.RELATED_ID = T2.id
                    JOIN innovator.IN_SURVEY_OPTION AS T3 WITH(NOLOCK)
                    ON T2.id = T3.SOURCE_ID
                    LEFT JOIN
                    (
                    	SELECT '{#in_property}' as in_property, count(*) AS reg_curr_qty 
                    	FROM 
                    	(
                    	    SELECT in_index FROM innovator.in_meeting_user　WITH(NOLOCK)
                            WHERE SOURCE_ID='{#meeting_id}' 
                            AND IN_CURRENT_ORG ='{#in_current_org}'
                            AND {#in_property} ='{#in_label}' 
                            AND IN_NOTE_STATE in('official','waiting')
                            AND IN_INDEX != '{#chk_index}' 
                            GROUP BY in_index
                    	) AS List
                    	
                    ) AS T4
                    ON t2.in_property = T4.in_property
                    WHERE  T1.SOURCE_ID ='{#meeting_id}' 
                    AND t2.in_property='{#in_property}' 
                    AND IN_LABEL='{#in_label}'  
                    AND  ISNULL(in_limit_current,-1) >= 0　
                    ORDER BY t2.in_property";
            
            sql = sql.Replace("{#in_property}",in_property)
                    .Replace("{#meeting_id}",meeting_id)
                    .Replace("{#in_label}",in_label)
                    .Replace("{#in_current_org}",in_current_org)
                    .Replace("{#chk_index}",chk_index);

            Item itmCurr = inn.applySQL(sql);
                    
            if (itmCurr.getItemCount() > 0 && !itmCurr.isError())
            {
                string in_limit_current = itmCurr.getProperty("in_limit_current",""); //限制單位人數
                int reg_curr_qty = GetIntVal(itmCurr.getProperty("reg_curr_qty",""));//已報單位人數
                question = itmCurr.getProperty("in_questions","");
                int limit_curr_qty = GetIntVal(in_limit_current);
                if(reg_curr_qty >= limit_curr_qty){
                     return "<span style='color:red;'>報名失敗</span>，<br>" + question + "：" + in_label +"，單位可報人數已滿。<br>";
                }
            }
            
            
            //判斷項目正取與備取人數
            sql = @" SELECT t2.in_property, IN_LABEL, in_admission, IN_QUESTIONS, reg_adm_qty , reg_pre_qty,in_preparation
                    FROM innovator.IN_MEETING_SURVEYS AS T1 WITH(NOLOCK)
                    JOIN  innovator.IN_SURVEY AS T2 WITH(NOLOCK)
                    ON T1.RELATED_ID = T2.id
                    JOIN innovator.IN_SURVEY_OPTION AS T3 WITH(NOLOCK)
                    ON T2.id = T3.SOURCE_ID
                    LEFT JOIN
                    (
                    	SELECT '{#in_property}' as in_property, count(*) AS reg_adm_qty 
                    	FROM 
                    	(
                    	    SELECT in_index FROM  innovator.in_meeting_user　WITH(NOLOCK)
                            WHERE SOURCE_ID='{#meeting_id}' 
                            AND {#in_property} ='{#in_label}' 
                            AND  IN_NOTE_STATE ='official'
                            AND IN_INDEX != '{#chk_index}'
                            GROUP BY in_index
                    	) AS list
                    ) AS T4
                    ON t2.in_property = T4.in_property
                    LEFT JOIN
                    (
                    	SELECT '{#in_property}' as in_property, count(*) AS reg_pre_qty 
                    	FROM 
                    	(
                    	    SELECT in_index FROM innovator.in_meeting_user　WITH(NOLOCK)
                            WHERE SOURCE_ID='{#meeting_id}' 
                            AND {#in_property} ='{#in_label}' 
                            AND  IN_NOTE_STATE ='waiting'
                            AND IN_INDEX != '{#chk_index}'
                            GROUP BY in_index
                    	) AS list
                    ) AS T5
                    ON t2.in_property = T5.in_property
                    WHERE  T1.SOURCE_ID ='{#meeting_id}' 
                    AND t2.in_property='{#in_property}' 
                    AND IN_LABEL='{#in_label}'  
                    AND  ISNULL(in_admission,-1) >= 0　
                    ORDER BY t2.in_property";
            
            sql = sql.Replace("{#in_property}",in_property)
                    .Replace("{#meeting_id}",meeting_id)
                    .Replace("{#in_label}",in_label)
                    .Replace("{#chk_index}",chk_index);
                    

            Item itmSurvery = inn.applySQL(sql);
                    
            if (itmSurvery.getItemCount() > 0 && !itmSurvery.isError())
            {
                
                string in_admission = itmSurvery.getProperty("in_admission",""); //限制正取人數
                int reg_adm_qty = GetIntVal(itmSurvery.getProperty("reg_adm_qty",""));//正取人數
                string in_preparation = itmSurvery.getProperty("in_preparation",""); //限制備取人數
                int reg_pre_qty = GetIntVal(itmSurvery.getProperty("reg_pre_qty",""));//備取人數
                question = itmSurvery.getProperty("in_questions","");
                
                int limit_adm_qty = GetIntVal(in_admission);
                int limit_pre_qty = GetIntVal(in_preparation);
                if(reg_adm_qty >= limit_adm_qty){
                    if (reg_pre_qty >= limit_pre_qty)
                    {
                        return "<span style='color:red;'>報名失敗</span>，<br>" + question + "：" + in_label +"，人數已滿。<br>";
                    }
                    else{
                        return "報名結果為<span style='color:red;'>備取</span>，<br>" + question + "：" + in_label +"，正取人數已滿。<br>";
                    }
                }
                
            }
            return "";
        }
        
        

        private int GetIntVal(string value, int def = 0)
        {
            int result = def;
            Int32.TryParse(value, out result);
            return result;
                #endregion MethodCode
            }
        }
    }
}
