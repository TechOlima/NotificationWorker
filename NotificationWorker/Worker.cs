using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NotificationWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration Configuration;
        private string QuenueServiceUrl;
        private string NotificationServiceUrl;

        public Worker(ILogger<Worker> logger, IConfiguration configRoot)
        {
            _logger = logger;
            Configuration = configRoot;
            QuenueServiceUrl = Configuration.GetValue<string>("QuenueServiceUrl");
            NotificationServiceUrl = Configuration.GetValue<string>("NotificationServiceUrl");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            HttpClient httpClient = new HttpClient();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);

                //SMS
                //получаем неотправленные сообщения
                using HttpRequestMessage requestCountSMS = new HttpRequestMessage(HttpMethod.Get, QuenueServiceUrl + "/api/Azure/GetQueueLength?queueName=sms");
                using HttpResponseMessage responseCountSMS = await httpClient.SendAsync(requestCountSMS);
                string requestCountSMSStr = responseCountSMS.RequestMessage.ToString();
                var responseCountSmsStr = await responseCountSMS.Content.ReadAsStringAsync();
                int countSms = int.Parse(responseCountSmsStr);

                //_logger.LogInformation("В очереди на отправку - " + countSms.ToString() + " сообщений.");

                if (countSms > 0)
                {
                    //получаем сообщение для отправки
                    using HttpRequestMessage requestSms = new HttpRequestMessage(HttpMethod.Post, QuenueServiceUrl + "/api/Azure/PeekMessage");

                    Dictionary<string, string> dataSms = new Dictionary<string, string>
                    {
                        ["queueName"] = "sms",
                        ["maxMessages"] = "1"
                    };
                    HttpContent contentFormSms = new FormUrlEncodedContent(dataSms);
                    requestSms.Content = contentFormSms;
                    using HttpResponseMessage responseSms = await httpClient.SendAsync(requestSms);
                    var responseStrSms = await responseSms.Content.ReadAsStringAsync();

                    string resp = Regex.Unescape(responseStrSms);

                    ICollection<Message>? messages = await responseSms.Content.ReadFromJsonAsync<ICollection<Message>>();
                        
                    if (messages?.Count() > 0)
                    {
                        //получили сообщение
                        Message message = messages.ToArray()[0];

                        //_logger.LogInformation("Получили сообщение - " + message.id + ". Текст:" + message.body);

                        //получаем содержимое
                        BodyContent? bodyContent = JsonConvert.DeserializeObject<BodyContent>(message.body);

                        if (bodyContent != null && !String.IsNullOrEmpty(bodyContent.Adress) && !String.IsNullOrEmpty(bodyContent.Message))
                        {
                            //отправляет задание на отправку смс
                            using HttpRequestMessage requestSmsSend = new HttpRequestMessage(HttpMethod.Post, NotificationServiceUrl + "/api/Notification/SendSMS");
                            Dictionary<string, string> dataSmsSend = new Dictionary<string, string>
                            {
                                ["phones"] = bodyContent.Adress,
                                ["mes"] = bodyContent.Message
                            };
                            HttpContent contentFormSmsSend = new FormUrlEncodedContent(dataSmsSend);
                            requestSmsSend.Content = contentFormSmsSend;
                            //отправили сообщение
                            using HttpResponseMessage responseSmsSend = await httpClient.SendAsync(requestSmsSend);

                            //if (responseSmsSend.IsSuccessStatusCode) _logger.LogInformation("Отправили сообщение - " + bodyContent.Adress + ". Текст:" + bodyContent.Message);

                            //удаляем сообщение из очереди
                            using HttpRequestMessage requestSmsSendDelete = new HttpRequestMessage(HttpMethod.Delete, QuenueServiceUrl + "/api/Azure/DequeueMessage");
                            Dictionary<string, string> dataSmsSendDelete = new Dictionary<string, string>
                            {
                                ["queueName"] = "sms",
                                ["messageId"] = message.id
                            };
                            HttpContent contentFormSmsSendDelete = new FormUrlEncodedContent(dataSmsSendDelete);
                            requestSmsSendDelete.Content = contentFormSmsSendDelete;
                            //отправили сообщение
                            using HttpResponseMessage responseSmsSendDelete = await httpClient.SendAsync(requestSmsSendDelete);

                            //if (responseSmsSendDelete.IsSuccessStatusCode) _logger.LogInformation("Удалили сообщение - " + message.id);
                            //Отправили и удалили сообщение
                        }
                        else
                        {
                            //_logger.LogInformation("Не удалось расшифровать сообщение - " + responseStrSms);
                        }
                    }
                    //else _logger.LogInformation("Не удалось получить сообщения");
                }

                //Email
                //получаем неотправленные сообщения
                using HttpRequestMessage requestCountEmail = new HttpRequestMessage(HttpMethod.Get, QuenueServiceUrl + "/api/Azure/GetQueueLength?queueName=email");
                using HttpResponseMessage responseCountEmail = await httpClient.SendAsync(requestCountEmail);
                string requestCountEmailStr = responseCountEmail.RequestMessage.ToString();
                var responseCountEmailStr = await responseCountEmail.Content.ReadAsStringAsync();
                int countEmail = int.Parse(responseCountEmailStr);

                //_logger.LogInformation("В очереди на отправку - " + countSms.ToString() + " почтовых сообщений.");

                if (countEmail > 0)
                {
                    //получаем сообщение для отправки
                    using HttpRequestMessage requestEmail = new HttpRequestMessage(HttpMethod.Post, QuenueServiceUrl + "/api/Azure/PeekMessage");

                    Dictionary<string, string> dataEmail = new Dictionary<string, string>
                    {
                        ["queueName"] = "email",
                        ["maxMessages"] = "1"
                    };
                    HttpContent contentFormEmail = new FormUrlEncodedContent(dataEmail);
                    requestEmail.Content = contentFormEmail;
                    using HttpResponseMessage responseEmail = await httpClient.SendAsync(requestEmail);
                    var responseStrEmail = await responseEmail.Content.ReadAsStringAsync();

                    ICollection<Message>? messagesEmail = await responseEmail.Content.ReadFromJsonAsync<ICollection<Message>>();
                                        
                    if (messagesEmail?.Count() > 0)
                    {
                        //получили сообщение
                        Message messageEmail = messagesEmail.ToArray()[0];

                        //_logger.LogInformation("Получили сообщение - " + messageEmail.id + ". Текст:" + messageEmail.body);

                        //получаем содержимое
                        BodyContent? bodyContentEmail = JsonConvert.DeserializeObject<BodyContent>(messageEmail.body);

                        if (bodyContentEmail != null && !String.IsNullOrEmpty(bodyContentEmail.Adress) && !String.IsNullOrEmpty(bodyContentEmail.Message))
                        {
                            //отправляет задание на отправку смс
                            using HttpRequestMessage requestEmailSend = new HttpRequestMessage(HttpMethod.Post, NotificationServiceUrl + "/api/Notification/SendEmail");
                            Dictionary<string, string> dataEmailSend = new Dictionary<string, string>
                            {
                                ["toEmail"] = bodyContentEmail.Adress,
                                ["caption"] = "Уведомления о статусе заказа",
                                ["message"] = bodyContentEmail.Message,
                                ["fromName"] = "Магазин украшений Олима"
                            };
                            HttpContent contentFormEmailSend = new FormUrlEncodedContent(dataEmailSend);
                            requestEmailSend.Content = contentFormEmailSend;
                            //отправили сообщение
                            using HttpResponseMessage responseEmailSend = await httpClient.SendAsync(requestEmailSend);

                            //if (responseEmailSend.IsSuccessStatusCode) _logger.LogInformation("Отправили сообщение - " + bodyContentEmail.Adress + ". Текст:" + bodyContentEmail.Message);

                            //удаляем сообщение из очереди
                            using HttpRequestMessage requestEmailSendDelete = new HttpRequestMessage(HttpMethod.Delete, QuenueServiceUrl + "/api/Azure/DequeueMessage");
                            Dictionary<string, string> dataEmailSendDelete = new Dictionary<string, string>
                            {
                                ["queueName"] = "email",
                                ["messageId"] = messageEmail.id
                            };
                            HttpContent contentFormEmailSendDelete = new FormUrlEncodedContent(dataEmailSendDelete);
                            requestEmailSendDelete.Content = contentFormEmailSendDelete;
                            //отправили сообщение
                            using HttpResponseMessage responseEmailSendDelete = await httpClient.SendAsync(requestEmailSendDelete);

                            //if (responseEmailSendDelete.IsSuccessStatusCode) _logger.LogInformation("Удалили сообщение - " + messageEmail.id);
                            //Отправили и удалили сообщение
                        }
                        else
                        {
                            //_logger.LogInformation("Не удалось расшифровать сообщение - " + responseStrEmail);
                        }
                    }
                    //else _logger.LogInformation("Не удалось получить сообщения");
                }
            }
        }
    }
}