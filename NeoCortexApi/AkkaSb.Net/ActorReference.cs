﻿using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaSb.Net
{
    public class ActorReference
    {
        public const string cActorType = "ActorType";
        public const string cActorId = "ActorId";
        public const string cMsgType = "MsgType";
        public const string cExpectResponse = "ExpectResponse";
        public const string cReply = "ExpectResponse";

        public TimeSpan MaxProcessingTime { get; set; } 


        internal QueueClient RequestMsgSenderClient  { get; set; }

        private ConcurrentDictionary<string, Message> receivedMsgQueue ;

        private ManualResetEvent rcvEvent;

        private Type actorType;

        private ActorId actorId;

        private string remoteNodeName;

        private string replyQueueName;

        private string name;

        private ILogger logger;

        internal ActorReference(Type actorType, ActorId id, QueueClient requestMsgSenderClient,  string replyQueueName, ConcurrentDictionary<string, Message> receivedMsgQueue, ManualResetEvent rcvEvent, TimeSpan maxProcessingTime, string name,  ILogger logger)
        {
            this.logger = logger;
            this.name = name;
            this.actorType = actorType;
            this.actorId = id;
            this.RequestMsgSenderClient = requestMsgSenderClient;
            this.MaxProcessingTime = maxProcessingTime;
            this.receivedMsgQueue = receivedMsgQueue;
            this.rcvEvent = rcvEvent;
            this.replyQueueName = replyQueueName;

            logger?.LogInformation($"ActorReference ctor: Actor System: {this.name}, receivedMsgQueue instance: {receivedMsgQueue.GetHashCode()}");
        }


        public async Task<TResponse> Ask<TResponse>(object msg, TimeSpan? timeout = null)
        {
            var sbMsg = CreateMessage(msg, true, actorType, actorId);
            sbMsg.ReplyTo = this.replyQueueName;

            int retries = 5;

            while (true)
            {
                try
                {
                    await this.RequestMsgSenderClient.SendAsync(sbMsg);
                    break;
                }
                catch (Exception ex)
                {
                    if (retries > 0)
                    {
                        this?.logger.LogWarning($"Message timeout. {sbMsg.SessionId}");
                        Thread.Sleep(1000);
                        retries--;
                    }
                    else
                        throw;
                }
            }

            TResponse res = WaitOnResponse<TResponse>(sbMsg, timeout);
            
            return res;
        }

        public async Task Tell(object msg, TimeSpan? timeout = null)
        {
            var sbMsg = CreateMessage(msg, false, actorType, actorId);

            await this.RequestMsgSenderClient.SendAsync(sbMsg);
        }

   

        #region Private Methods

        private TResponse WaitOnResponse<TResponse>(Message sbMsg, TimeSpan? timeout = null)
        {
            logger?.LogInformation($"ActorReference: Actor System: {this.name}, receivedMsgQueue instance: {receivedMsgQueue.GetHashCode()}");
            DateTime entered = DateTime.Now;

            TResponse msg = default(TResponse);
            
            while (DateTime.Now < entered.AddMinutes(timeout.HasValue ? timeout.Value.TotalMinutes : this.MaxProcessingTime.TotalMinutes))
            {
                if (rcvEvent.WaitOne())
                {
                    Message sbRcvMsg;

                    if (receivedMsgQueue.TryRemove(sbMsg.MessageId, out sbRcvMsg))
                    {
                        msg = DeserializeMsg<TResponse>(sbRcvMsg.Body);
                        break;
                    }

                    rcvEvent.Reset();
                }
                else
                    throw new TimeoutException($"Actor system didn't sent any response after specified timeout interval. This can be a communication issue or actor is still running!");
            }
            
            if(msg == null)
                throw new TimeoutException($"Actor system didn't response after specified timeout interval. This can be a communication issue or actor is still running!");

            return msg;
        }
              

        internal static Message CreateMessage(object msg, bool expectResponse, Type actorType, ActorId actorId)
        {
            Message sbMsg = new Message(SerializeMsg(msg));
         
            sbMsg.UserProperties.Add(cActorType, actorType.AssemblyQualifiedName);
            sbMsg.UserProperties.Add(cMsgType, msg.GetType().AssemblyQualifiedName);
            sbMsg.UserProperties.Add(cActorId, (string)actorId);
            sbMsg.UserProperties.Add(cExpectResponse, (bool)expectResponse);

            sbMsg.SessionId = $"{actorType.Name}/{actorId}";
            sbMsg.MessageId = $"{sbMsg.SessionId}/{Guid.NewGuid().ToString()}";

            return sbMsg;
        }

        internal static Message CreateResponseMessage(object msg, string replyToMsgId, Type actorType, ActorId actorId)
        {
            Message sbMsg = CreateMessage(msg, false, actorType, actorId);
            sbMsg.CorrelationId = replyToMsgId;
            // Response messages do not use sessions.
            //sbMsg.SessionId = $"{actorType}/{actorId}";

            return sbMsg;
        }

        internal static byte[] SerializeMsg(object msg)
        {
            JsonSerializerSettings sett = new JsonSerializerSettings();
            sett.TypeNameHandling = TypeNameHandling.All;
            
            var strObj = JsonConvert.SerializeObject(msg, sett);

            return UTF8Encoding.UTF8.GetBytes(strObj);
        }

        internal static T DeserializeMsg<T>(byte[] msg)
        {
            JsonSerializerSettings sett = new JsonSerializerSettings();
            sett.TypeNameHandling = TypeNameHandling.All;
            var strObj = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(msg), sett);

            return strObj;
        }

       

        #endregion
    }
}
