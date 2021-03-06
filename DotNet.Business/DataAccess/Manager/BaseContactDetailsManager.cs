﻿//-----------------------------------------------------------------------
// <copyright file="BaseContactDetailsManager.cs" company="Hairihan">
//     Copyright (C) 2016 , All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;

namespace DotNet.Business
{
    using DotNet.Utilities;
    using DotNet.Model;

    /// <summary>
    /// BaseContactDetailsManager
    /// 内部邮件明细表
    ///
    /// 修改记录
    ///
    ///     2009-11-04 版本：1.3 Jirigala  增加分类功能，部门公告与邮件有用相同的界面，相同的数据结构。 
    ///     2009-08-27 版本：1.2 Jirigala  已经被删除的用户、已经被设置为无效的用户不应该发邮件。 
    ///     2009-03-24 版本：1.1 Chenbotao 添加发送邮件方法。 
    ///		2009-03-18 版本：1.0 Chenbotao 创建主键。
    ///
    /// 版本：1.3
    ///
    /// <author>
    ///		<name>Jirigala</name>
    ///		<date>2009-11-04</date>
    /// </author>
    /// </summary>
    /// 
    public partial class BaseContactDetailsManager : BaseManager, IBaseManager
    {
        #region public int GetNewCount(string userId) 获取新信息个数
        /// <summary>
        /// 获取新信息个数
        /// 要记得去掉已删除的邮件
        /// </summary>
        /// <returns>记录个数</returns>
        public int GetNewCount(string userId)
        {
            int returnValue = 0;
            string sqlQuery = " SELECT COUNT(*) "
                            + "   FROM " + base.CurrentTableName
                            + "  WHERE (" + BaseContactDetailsEntity.FieldDeletionStateCode + " = 0 ) "
                            + "        AND (" + BaseContactDetailsEntity.FieldIsNew + " = " + ((int)MessageStateCode.New).ToString() + " ) "
                            + "        AND (" + BaseContactDetailsEntity.FieldReceiverId + " = '" + userId + "' ) "
                            + "        AND (" + BaseContactDetailsEntity.FieldCategory + " = 'User' ) ";
            object returnObject = DbHelper.ExecuteScalar(sqlQuery);
            if (returnObject != null)
            {
                returnValue = int.Parse(returnObject.ToString());
            }
            return returnValue;
        }
        #endregion

        #region public int GetNewCount(string userId, string category) 获取新信息个数
        /// <summary>
        /// 获取新信息个数
        /// 要记得去掉已删除的邮件
        /// </summary>
        /// <param name="userId">用户主键</param>
        /// <param name="category">分类类别</param>
        /// <returns>记录个数</returns>
        public int GetNewCount(string userId, string category)
        {
            if (String.IsNullOrEmpty(category))
            {
                return this.GetNewCount(userId);
            }
            int returnValue = 0;

            string sqlQuery = " SELECT COUNT(*) "
                            + "   FROM " + BaseContactEntity.TableName
                            + "  WHERE " + BaseContactEntity.FieldParentId + "='" + category + "'"
                            + "        AND Id IN ( "
                                                + " SELECT " + BaseContactDetailsEntity.FieldContactId
                                                + "   FROM " + base.CurrentTableName
                                                + "  WHERE (" + BaseContactDetailsEntity.FieldDeletionStateCode + " = 0 ) "
                                                + "        AND (" + BaseContactDetailsEntity.FieldIsNew + " = " + ((int)MessageStateCode.New).ToString() + " ) "
                                                + "        AND (" + BaseContactDetailsEntity.FieldReceiverId + " = '" + userId + "' ) "
                                                + "        AND (" + BaseContactDetailsEntity.FieldCategory + " = 'User' ) "
                                                + " ) ";
            object returnObject = DbHelper.ExecuteScalar(sqlQuery);
            if (returnObject != null)
            {
                returnValue = int.Parse(returnObject.ToString());
            }
            return returnValue;
        }
        #endregion

        /// <summary>
        /// 转发给别人看
        /// </summary>
        /// <param name="contactId">内部联络单主键</param>
        /// <param name="receiverIds">送给</param>
        /// <returns>影响行数</returns>
        public int AddReceivers(string contactId, string[] receiverIds)
        {
            int returnValue = 0;
            BaseUserManager userManager = new BaseUserManager(DbHelper, UserInfo);
            BaseUserEntity useEntity = null;
            for (int i = 0; i < receiverIds.Length; i++)
            {
                if (!this.Exists(new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldContactId, contactId)
                    , new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldReceiverId, receiverIds[i])))
                {
                    useEntity = userManager.GetObject(receiverIds[i]);
                    // 是有效的用户，而且是未必删除的用户才发邮件
                    if (useEntity.Enabled == 1 && useEntity.DeletionStateCode == 0)
                    {
                        this.AddReceiver(useEntity, contactId, receiverIds[i]);
                        returnValue++;
                    }
                }
            }
            // 这里需要重新计算发送给了几个人，几个人已经阅读的功能
            this.SetReadState(contactId);
            return returnValue;
        }

        public string AddReceiver(string contactId, string receiverId)
        {
            string returnValue = string.Empty;
            // 需要判断是否存在
            if (this.Exists(new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldContactId,contactId)
                , new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldReceiverId, receiverId)
                , new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldCategory, "User")))
            {
                return string.Empty;
            }
            BaseUserManager userManager = new BaseUserManager(DbHelper, UserInfo);
            BaseUserEntity useEntity = userManager.GetObject(receiverId);
            BaseContactDetailsEntity contactDetailsEntity = new BaseContactDetailsEntity();
            // 这里一定要给个不可猜测的主键，为了提高安全性
            contactDetailsEntity.Id = Guid.NewGuid().ToString("N");
            contactDetailsEntity.ContactId = contactId;
            contactDetailsEntity.Category = "User";
            contactDetailsEntity.ReceiverId = useEntity.Id.ToString();
            contactDetailsEntity.ReceiverRealName = useEntity.RealName;
            contactDetailsEntity.IsNew = 0;
            contactDetailsEntity.Enabled = 1;
            contactDetailsEntity.NewComment = 0;
            returnValue = this.Add(contactDetailsEntity);
            // 这里需要重新计算发送给了几个人，几个人已经阅读的功能
            this.SetReadState(contactId);
            return returnValue;
        }

        private string AddReceiver(BaseUserEntity useEntity, string contactId, string receiverId)
        {
            BaseContactDetailsEntity contactDetailsEntity = new BaseContactDetailsEntity();
            // 这里一定要给个不可猜测的主键，为了提高安全性
            contactDetailsEntity.Id = Guid.NewGuid().ToString("N");
            contactDetailsEntity.ContactId = contactId;
            contactDetailsEntity.Category = "User";
            contactDetailsEntity.ReceiverId = useEntity.Id.ToString();
            contactDetailsEntity.ReceiverRealName = useEntity.RealName;
            contactDetailsEntity.IsNew = 1;
            contactDetailsEntity.Enabled = 1;
            contactDetailsEntity.NewComment = 0;
            return this.Add(contactDetailsEntity, false);
        }

        /// <summary>
        /// 移除收件人
        /// </summary>
        /// <param name="ContactId">内部联络单主键</param>
        /// <param name="receiverID">接收者主键</param>
        /// <returns>影响行数</returns>
        public int RemoveReceiver(string contactId, string receiverId)
        {
            int returnValue = 0;
            returnValue = this.Delete(new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldContactId, contactId), new KeyValuePair<string, object>(BaseContactDetailsEntity.FieldReceiverId, receiverId));
            // 这里需要重新计算发送给了几个人，几个人已经阅读的功能
            this.SetReadState(contactId);
            return returnValue;
        }

        /// <summary>
        /// 已阅读人数等计算
        /// </summary>
        /// <param name="id">主键</param>
        /// <returns>影响行数</returns>
        private int SetReadState(string id)
        {
            int returnValue = 0;
            // 发送人数
            string sqlQuery = " UPDATE BaseContact "
                            + "    SET SendCount = ( "
                            + "                      SELECT COUNT(*) "
                            + "                        FROM BaseContactDetails "
                            + "                       WHERE (Enabled = 1 AND ContactId = '" + id + "')) "
                            + "  WHERE Id = '" + id + "' ";
            returnValue += DbHelper.ExecuteNonQuery(sqlQuery);

            // 已阅读人数
            sqlQuery = " UPDATE BaseContact "
                     + "    SET ReadCount = ( "
                     + "                      SELECT COUNT(IsNew) "
                     + "                        FROM BaseContactDetails "
                     + "                       WHERE (Enabled = 1 AND ContactId = '" + id + "') AND (IsNew = 0)) "
                     + "  WHERE Id = '" + id + "'";
            returnValue += DbHelper.ExecuteNonQuery(sqlQuery);

            return returnValue;
        }

        #region public DataTable Read(string id) 阅读短信
        /// <summary>
        /// 阅读短信
        /// </summary>
        /// <param name="id">短信ID</param>
        /// <returns>数据集</returns>
        public DataTable Read(string id)
        {
            // 阅读处理
            DataTable dataTable = this.GetDataTableById(id);
            BaseContactDetailsEntity contactDetailsEntity = new BaseContactDetailsEntity(dataTable);
            this.OnRead(contactDetailsEntity);
            // 返回结果
            BaseContactManager contactManager = new BaseContactManager(DbHelper, UserInfo);
            return contactManager.GetDataTableById(contactDetailsEntity.ContactId);
        }
        #endregion

        #region private int OnRead(BaseContactDetailsEntity contactDetailsEntity) 阅读短信后设置状态值和阅读次数
        /// <summary>
        /// 阅读短信后设置状态值和阅读次数
        /// </summary>
        /// <param name="contactDetailsEntity">实体</param>
        /// <returns>影响的条数</returns>
        private int OnRead(BaseContactDetailsEntity contactDetailsEntity)
        {
            int returnValue = 0;
            // 设置邮件发送记录为已读状态
            // 标记为不是新邮件
            contactDetailsEntity.IsNew = 0;
            // 标记为无新评论
            contactDetailsEntity.NewComment = 0;
            // 最后访问日期
            contactDetailsEntity.LastViewDate = DateTime.Now.ToString();
            // 最后访问地址
            contactDetailsEntity.LastViewIP = UserInfo.IPAddress;
            // 更新实体
            returnValue += this.Update(contactDetailsEntity);
            // 阅读新邮件人数加
            this.SetReadState(contactDetailsEntity.ContactId);
            return returnValue;
        }
        #endregion

        /// <summary>
        /// 邮件有评论时要进行的操作
        /// </summary>
        /// <param name="detailsID">邮件主键</param>
        /// <returns>影响行数</returns>
        public int OnCommnet(string detailsId)
        {
            int returnValue = 0;
            BaseContactDetailsEntity contactDetailsEntity = this.GetEntity(detailsId);            
            // 更新子表的其他人的通知，首先是有效的邮件，其次不是新邮件，其次不是自己的邮件
            string sqlQuery = " UPDATE BaseContactDetails "
                            + "    SET NewComment = 1 "
                            + "  WHERE (Enabled = 1) "
                            + "        AND (IsNew = 0) "
                            + "        AND (ContactId = '" + contactDetailsEntity.ContactId + "') "
                            + "        AND (ID <> '" + detailsId + "')";
            returnValue += DbHelper.ExecuteNonQuery(sqlQuery);
            BaseContactManager contactManager = new BaseContactManager(DbHelper, UserInfo);
            BaseContactEntity contactEntity = contactManager.GetEntity(contactDetailsEntity.ContactId);
            // 更新主表状态，评论人、评论时间发布上去
            contactEntity.CommentDate = DateTime.Now;
            contactEntity.CommentUserId = UserInfo.Id;
            contactEntity.CommentUserRealName = UserInfo.RealName;
            returnValue += contactManager.Update(contactEntity);
            return returnValue;
        }
    }
}
