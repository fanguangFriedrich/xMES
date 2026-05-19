using Infrastructure.Cache;
using Jusoft.DingtalkStream.Core;
using OpenAuth.App.Request;
using OpenAuth.Repository.Domain.DingTalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.App.DingTalk
{
    public class DingTalkStreamMessageHandler : IDingtalkStreamMessageHandler
    {
        private readonly ICacheContext _cache;
        private readonly DingTalkApp _dingTalkApp;
        private readonly UserManagerApp _userManagerApp;
        private readonly OrgManagerApp _orgManagerApp;
        private readonly DingTalkLoginApp _dingTalkLoginApp;

        public DingTalkStreamMessageHandler(
            ICacheContext cache,
            DingTalkApp dingTalkApp,
            UserManagerApp userManagerApp,
            OrgManagerApp orgManagerApp,
            DingTalkLoginApp dingTalkLoginApp)
        {
            _cache = cache;
            _dingTalkApp = dingTalkApp;
            _userManagerApp = userManagerApp;
            _orgManagerApp = orgManagerApp;
            _dingTalkLoginApp=dingTalkLoginApp;
        }

        public async Task HandleMessage(MessageEventHanderArgs e)
        {
            var replyMessageData = string.Empty;

            switch (e.Type)
            {
                case SubscriptionType.EVENT:
                    {
                        var eventId = e.Headers.Payload.GetProperty("eventId").GetString();
                        var eventType = e.Headers.Payload.GetProperty("eventType").GetString();

                        // 去重
                        var cacheKey = $"dingtalk:event:{eventId}";
                        var existing = _cache.Get<string>(cacheKey);
                        if (existing != null)
                        {
                            replyMessageData = await DingtalkStreamUtilities
                                .CreateReply_EventSuccess_MessageData("success");
                            break;
                        }
                        _cache.Set(cacheKey, "1", DateTime.Now.AddHours(24));

                        switch (eventType)
                        {
                            case "user_add_org":
                                var addData = System.Text.Json.JsonSerializer
                                    .Deserialize<UserChangeEventData>(e.Data);
                                await HandleUserAdded(addData);
                                break;

                            case "user_leave_org":
                                var leaveData = System.Text.Json.JsonSerializer
                                    .Deserialize<UserChangeEventData>(e.Data);
                                await HandleUserLeave(leaveData);
                                break;

                            case "user_modify_org":
                                break;
                        }

                        replyMessageData = await DingtalkStreamUtilities
                            .CreateReply_EventSuccess_MessageData("success");
                        break;
                    }

                case SubscriptionType.CALLBACK:
                    replyMessageData = DingtalkStreamUtilities
                        .CreateReply_Callback_MessageData("自定义回调结果");
                    break;
            }

            var replyMessage = await DingtalkStreamUtilities
                .CreateReplyMessage(e.Headers.MessageId, replyMessageData);
            await e.Reply(replyMessage);
        }

        private async Task HandleUserAdded(UserChangeEventData data)
        {
            foreach (var userId in data.UserId)
            {
                try
                {
                    // 1. 获取钉钉用户详情
                    var userDetail = await _dingTalkApp.GetUserDetailInfoByUserIdAsync(userId);
                    if (userDetail == null)
                    {
                        Console.WriteLine($"获取用户详情为空 userId={userId}");
                        continue;
                    }

                    var usr= await _dingTalkLoginApp.LoginOrRegisterByDingTalkAsync(userDetail, enableRegister: true);

                    if (usr!=null)
                    {
                        Console.WriteLine($"新增用户成功: {userDetail.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"新增用户失败 userId={userId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理新增用户失败 userId={userId}: {ex.Message}");
                }
            }
        }

        private async Task HandleUserLeave(UserChangeEventData data)
        {
            foreach (var userId in data.UserId)
            {
                try
                {
                    _userManagerApp.DeleteByDingTalkUserId(userId);
                    Console.WriteLine($"删除用户成功 dingTalkUserId={userId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理离职用户失败 userId={userId}: {ex.Message}");
                }
            }
        }
    }

    public class UserChangeEventData
    {
        [JsonPropertyName("timeStamp")]
        public string TimeStamp { get; set; }

        [JsonPropertyName("eventId")]
        public string EventId { get; set; }

        [JsonPropertyName("optStaffId")]
        public string OptStaffId { get; set; }

        [JsonPropertyName("userId")]
        public List<string> UserId { get; set; }
    }
}
