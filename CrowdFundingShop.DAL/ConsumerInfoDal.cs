﻿using CrowdFundingShop.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace CrowdFundingShop.DAL
{
    public class ConsumerInfoDal
    {
        #region 修改
        public static bool Update(Model.ConsumerInfo entity)
        {
            string sql = @"UPDATE ConsumerInfo SET NickName=@NickName,Phone=@Phone,[Address]=@Address,PostCode=@PostCode WHERE ID=@ID";
            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter() { ParameterName = "@ID", Value = entity.ID });
            parameters.Add(new SqlParameter() { ParameterName = "@NickName", Value = entity.NickName });
            parameters.Add(new SqlParameter() { ParameterName = "@Phone", Value = entity.Phone });
            parameters.Add(new SqlParameter() { ParameterName = "@Address", Value = entity.Address });
            parameters.Add(new SqlParameter() { ParameterName = "@PostCode", Value = entity.PostCode });
            int i = SqlHelper.ExecuteNonQuery(sql, parameters.ToArray());
            return i > 0 ? true : false;
        }
        #endregion

        #region 查找
        public static Model.ConsumerInfo GetByID(long id)
        {
            var sql = @"
                              SELECT 
                                    ID
                                    ,WeiXinAccount
                                    ,NickName
                                    ,Phone
                                    ,HeadIcon
                                    ,[Address]
                                    ,PostCode
                                FROM ConsumerInfo 
                                WHERE ID=@ID";
            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter() { ParameterName = "@ID", Value = id });
            sql = string.Format(sql);
            DataTable dataTable = SqlHelper.ExecuteDataTable(sql, parameters.ToArray());
            if (dataTable.Rows.Count > 0)
            {
                var row = dataTable.Rows[0];
                return new Model.ConsumerInfo()
                {
                    ID = Converter.TryToInt32(row["ID"], -1),
                    WeiXinAccount = Converter.TryToString(row["WeiXinAccount"], string.Empty),
                    NickName = Converter.TryToString(row["Nickname"], string.Empty),
                    Phone = Converter.TryToString(row["Phone"], string.Empty),
                    Address = Converter.TryToString(row["Address"], string.Empty),
                    PostCode = Converter.TryToString(row["PostCode"], string.Empty),
                };
            }
            return null;
        }
        #endregion
    }
}
