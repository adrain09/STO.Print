﻿//-----------------------------------------------------------------
// All Rights Reserved , Copyright (C) 2016 , Hairihan TECH, Ltd.  
//-----------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.ServiceModel;

namespace DotNet.IService
{
    using DotNet.Utilities;

    /// <summary>
    /// IExceptionService
    /// 
    /// 修改记录
    /// 
    ///		2008.04.02 版本：1.0 JiRiGaLa 添加接口定义。
    ///		
    /// <author>
    ///		<name>JiRiGaLa</name>
    ///		<date>2008.04.02</date>
    /// </author> 
    /// </summary>
    [ServiceContract]
    public interface IExceptionService
    {
        /// <summary>
        /// 获取异常列表
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <returns>数据表</returns>
        [OperationContract]
        DataTable GetDataTable(BaseUserInfo userInfo);

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="recordCount">记录数</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">每页显示</param>
        /// <param name="whereClause">条件</param>
        /// <param name="dbParameters">参数</param>
        /// <param name="order">排序</param>
        /// <returns>数据表</returns>
        [OperationContract]
        DataTable GetDataTableByPage(BaseUserInfo userInfo, out int recordCount, int pageIndex, int pageSize, string whereClause, List<KeyValuePair<string, object>> dbParameters, string order = null);

        /// <summary>
        /// 批量删除异常
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="id">主键</param>
        /// <returns>数据表</returns>
        [OperationContract]
        DataTable Delete(BaseUserInfo userInfo, string id);

        /// <summary>
        /// 批量删除异常
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="ids">主键数组</param>
        /// <returns>影响行数</returns>
        [OperationContract]
        int BatchDelete(BaseUserInfo userInfo, string[] ids);

        /// <summary>
        /// 清除
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <returns>影响行数</returns>
        [OperationContract]
        int Truncate(BaseUserInfo userInfo);
    }
}