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
                //�������� �������������� ���������
                using HttpRequestMessage requestCountSMS = new HttpRequestMessage(HttpMethod.Get, QuenueServiceUrl + "/api/Azure/GetQueueLength?queueName=sms");
                using HttpResponseMessage responseCountSMS = await httpClient.SendAsync(requestCountSMS);
                string requestCountSMSStr = responseCountSMS.RequestMessage.ToString();
                var responseCountSmsStr = await responseCountSMS.Content.ReadAsStringAsync();
                int countSms = int.Parse(responseCountSmsStr);

                //_logger.LogInformation("� ������� �� �������� - " + countSms.ToString() + " ���������.");

                if (countSms > 0)
                {
                    //�������� ��������� ��� ��������
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
                        //�������� ���������
                        Message message = messages.ToArray()[0];

                        //_logger.LogInformation("�������� ��������� - " + message.id + ". �����:" + message.body);

                        //�������� ����������
                        BodyContent? bodyContent = JsonConvert.DeserializeObject<BodyContent>(message.body);

                        if (bodyContent != null && !String.IsNullOrEmpty(bodyContent.Adress) && !String.IsNullOrEmpty(bodyContent.Message))
                        {
                            //���������� ������� �� �������� ���
                            using HttpRequestMessage requestSmsSend = new HttpRequestMessage(HttpMethod.Post, NotificationServiceUrl + "/api/Notification/SendSMS");
                            Dictionary<string, string> dataSmsSend = new Dictionary<string, string>
                            {
                                ["phones"] = bodyContent.Adress,
                                ["mes"] = bodyContent.Message
                            };
                            HttpContent contentFormSmsSend = new FormUrlEncodedContent(dataSmsSend);
                            requestSmsSend.Content = contentFormSmsSend;
                            //��������� ���������
                            using HttpResponseMessage responseSmsSend = await httpClient.SendAsync(requestSmsSend);

                            //if (responseSmsSend.IsSuccessStatusCode) _logger.LogInformation("��������� ��������� - " + bodyContent.Adress + ". �����:" + bodyContent.Message);

                            //������� ��������� �� �������
                            using HttpRequestMessage requestSmsSendDelete = new HttpRequestMessage(HttpMethod.Delete, QuenueServiceUrl + "/api/Azure/DequeueMessage");
                            Dictionary<string, string> dataSmsSendDelete = new Dictionary<string, string>
                            {
                                ["queueName"] = "sms",
                                ["messageId"] = message.id
                            };
                            HttpContent contentFormSmsSendDelete = new FormUrlEncodedContent(dataSmsSendDelete);
                            requestSmsSendDelete.Content = contentFormSmsSendDelete;
                            //��������� ���������
                            using HttpResponseMessage responseSmsSendDelete = await httpClient.SendAsync(requestSmsSendDelete);

                            //if (responseSmsSendDelete.IsSuccessStatusCode) _logger.LogInformation("������� ��������� - " + message.id);
                            //��������� � ������� ���������
                        }
                        else
                        {
                            //_logger.LogInformation("�� ������� ������������ ��������� - " + responseStrSms);
                        }
                    }
                    //else _logger.LogInformation("�� ������� �������� ���������");
                }

                //Email
                //�������� �������������� ���������
                using HttpRequestMessage requestCountEmail = new HttpRequestMessage(HttpMethod.Get, QuenueServiceUrl + "/api/Azure/GetQueueLength?queueName=email");
                using HttpResponseMessage responseCountEmail = await httpClient.SendAsync(requestCountEmail);
                string requestCountEmailStr = responseCountEmail.RequestMessage.ToString();
                var responseCountEmailStr = await responseCountEmail.Content.ReadAsStringAsync();
                int countEmail = int.Parse(responseCountEmailStr);

                //_logger.LogInformation("� ������� �� �������� - " + countSms.ToString() + " �������� ���������.");

                if (countEmail > 0)
                {
                    //�������� ��������� ��� ��������
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
                        //�������� ���������
                        Message messageEmail = messagesEmail.ToArray()[0];

                        //_logger.LogInformation("�������� ��������� - " + messageEmail.id + ". �����:" + messageEmail.body);

                        //�������� ����������
                        BodyContent? bodyContentEmail = JsonConvert.DeserializeObject<BodyContent>(messageEmail.body);

                        if (bodyContentEmail != null && !String.IsNullOrEmpty(bodyContentEmail.Adress) && !String.IsNullOrEmpty(bodyContentEmail.Message))
                        {
                            //���������� ������� �� �������� ���
                            using HttpRequestMessage requestEmailSend = new HttpRequestMessage(HttpMethod.Post, NotificationServiceUrl + "/api/Notification/SendEmail");
                            Dictionary<string, string> dataEmailSend = new Dictionary<string, string>
                            {
                                ["toEmail"] = bodyContentEmail.Adress,
                                ["caption"] = "����������� � ������� ������",
                                ["message"] = bodyContentEmail.Message,
                                ["fromName"] = "������� ��������� �����"
                            };
                            HttpContent contentFormEmailSend = new FormUrlEncodedContent(dataEmailSend);
                            requestEmailSend.Content = contentFormEmailSend;
                            //��������� ���������
                            using HttpResponseMessage responseEmailSend = await httpClient.SendAsync(requestEmailSend);

                            //if (responseEmailSend.IsSuccessStatusCode) _logger.LogInformation("��������� ��������� - " + bodyContentEmail.Adress + ". �����:" + bodyContentEmail.Message);

                            //������� ��������� �� �������
                            using HttpRequestMessage requestEmailSendDelete = new HttpRequestMessage(HttpMethod.Delete, QuenueServiceUrl + "/api/Azure/DequeueMessage");
                            Dictionary<string, string> dataEmailSendDelete = new Dictionary<string, string>
                            {
                                ["queueName"] = "email",
                                ["messageId"] = messageEmail.id
                            };
                            HttpContent contentFormEmailSendDelete = new FormUrlEncodedContent(dataEmailSendDelete);
                            requestEmailSendDelete.Content = contentFormEmailSendDelete;
                            //��������� ���������
                            using HttpResponseMessage responseEmailSendDelete = await httpClient.SendAsync(requestEmailSendDelete);

                            //if (responseEmailSendDelete.IsSuccessStatusCode) _logger.LogInformation("������� ��������� - " + messageEmail.id);
                            //��������� � ������� ���������
                        }
                        else
                        {
                            //_logger.LogInformation("�� ������� ������������ ��������� - " + responseStrEmail);
                        }
                    }
                    //else _logger.LogInformation("�� ������� �������� ���������");
                }
            }
        }
    }
}