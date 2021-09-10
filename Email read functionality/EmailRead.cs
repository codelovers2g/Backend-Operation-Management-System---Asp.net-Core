 public async void CreateRequestFromMail()
        {
            try
            {
                IUnityOfWork unityOfWork = new UnityOfWork(_context);
                ICustomersDetailsService _customersDetailsService = new CustomersDetailsService();
                EmailRequestVM emailRequestVM = new EmailRequestVM();
                RequestedData requestedData = new RequestedData();
                CreateNewRequest createNewRequest = new CreateNewRequest();
                Common.ViewModel.Attachments Attachments = new Common.ViewModel.Attachments();
                List<Ticket> Ticketlist = new List<Ticket>();
                BOS.StrarterCode.DAL.DataModels.RequestResponseTimeEntity RequestResponseTime = new BOS.StrarterCode.DAL.DataModels.RequestResponseTimeEntity();
                Request Requestdetails = new Request();
                Requestor requestor = new Requestor();
                Submitter submitter = new Submitter();
                Attachment emailAttachment = null;
                List<Request> RequestsByTicket = new List<Request>();
                List<Attachment> attachments = new List<Attachment>();

                //Start reading emails using 
                using (var client = new ImapClient())
                {
                    client.Connect("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
                    client.Authenticate(_configuration["ReadingMail:userName"].ToString(), _configuration["ReadingMail:password"].ToString());
                    client.Inbox.Open(FolderAccess.ReadWrite);

                    IList<UniqueId> uids = client.Inbox.Search(SearchQuery.NotSeen);
                    int num = uids.Count();
                    string path = _webHostEnvironment.WebRootPath + "\\EmailAttachments";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //To remover all available files from directory
                    //System.IO.DirectoryInfo di = new DirectoryInfo(path);

                    //foreach (FileInfo file in di.GetFiles())
                    //{
                    //    file.Delete();
                    //}

                    foreach (UniqueId uid in uids)
                    {
                        MimeMessage message = client.Inbox.GetMessage(uid);

                        emailRequestVM.EmailFrom = message.From.ToString();
                        emailRequestVM.EmailTo = message.To.ToString();
                        emailRequestVM.Subject = Convert.ToString(message.Subject) ?? "Empty Subject";
                        emailRequestVM.TextBody = Convert.ToString(message.TextBody) ?? "Empty TextBody";
                        emailRequestVM.BodyParts = Convert.ToString(message.BodyParts) ?? "Empty Body Parts";
                        emailRequestVM.CreatedDate = DateTime.Now.ToString("MMM dd, yyyy");
                        Guid newStatusId = new Guid("3d724868-62ab-4dc1-8ece-05e598a78c8c");

                        requestedData.CreatedDate = emailRequestVM.CreatedDate;

                        string[] emailFromParts = emailRequestVM.EmailTo.Split(".",2);
                        
                        //string[] emailFromParts = emailRequestVM.EmailTo.Split(".");
                        string[] Organization = emailFromParts[1].Split("@");
                        string nameOfOrganization = Organization[0].ToString().Trim().Replace("_", " ");

                        requestedData.CurrentRequestType = emailFromParts[0].Trim().ToString();
                        requestedData.DueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddThh:mmZ");
                        requestedData.ProrityLevel = "Low";
                        //requestedData.OrganizationName = Organization[0].ToString();
                        requestedData.OrganizationName = nameOfOrganization;
                        requestedData.Title = emailRequestVM.TextBody;
                        requestedData.StatusId = new Guid("55558081-1dd4-41dc-90b9-2d62ecd89051");


                        foreach (MimeEntity attachment in message.Attachments)
                        {
                            if (attachment.IsAttachment)
                            {
                                string dir = _webHostEnvironment.WebRootPath + "\\EmailAttachments";
                                if (!Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                var fileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name;
                                string filePath = Path.Combine(_webHostEnvironment.WebRootPath + "\\EmailAttachments", fileName);
                                
                                //to Store Attachment in application
                                using (var stream = File.Create(filePath))
                                {
                                    if (attachment is MessagePart)
                                    {
                                        var rfc822 = (MessagePart)attachment;
                                        rfc822.Message.WriteTo(stream);
                                    }
                                    else
                                    {
                                        var part = (MimeKit.MimePart)attachment;

                                        part.Content.DecodeTo(stream);
                                    }
                                }

                                FileInfo fileInfo = new FileInfo(filePath);
                                emailAttachment = new Attachment();
                                emailAttachment.FileName = Convert.ToString(fileName);
                                emailAttachment.FileUrl = Convert.ToString(filePath);
                                emailAttachment.FileType = Convert.ToString(attachment.ContentType.MimeType);
                                emailAttachment.FileSize = Convert.ToString(fileInfo.Length);

                                attachments.Add(emailAttachment);


                            }
                            else
                            {
                                ErrorLog.WriteLogFile("Error at Create Create file for email." + DateTime.Now.ToString(), "Current FileName: Sorry here no files available");
                            }

                        }

                        string dirError = _webHostEnvironment.WebRootPath + "\\ErrorLog";
                        if (!Directory.Exists(dirError))
                        {
                            Directory.CreateDirectory(dirError);
                        }

                        string RequesterUserEmail = string.Empty;
                        if (requestedData.CurrentRequestType.ToLower() == "it" || requestedData.CurrentRequestType.ToLower() == "service" || requestedData.CurrentRequestType.ToLower() == "supply")
                        {
                            //Start getting data from  database here
                            int position = emailRequestVM.EmailFrom.IndexOf("<");
                            int lengthOfString = emailRequestVM.EmailFrom.ToString().Length - 1;

                            if (position < 0)
                            {
                            }
                            else
                            {
                                string substr = emailRequestVM.EmailFrom.Substring(position + 1, (emailRequestVM.EmailFrom.ToString().Length - 1) - position);
                                string UserEmail = substr.Remove(substr.ToString().Length - 1, 1);
                                RequesterUserEmail = UserEmail;
                                Console.WriteLine(UserEmail);
                            }

                            //Get Customer details like CustomerId and other organization details
                            Common.ViewModel.CustomerVM customerVM = new Common.ViewModel.CustomerVM();
                            customerVM = _mapper.Map<Common.ViewModel.CustomerVM>(unityOfWork.CustomersDetailsRepository.FetchCustomerDetailsByName(requestedData.OrganizationName, _configuration));

                            if(customerVM == null) { continue; }

                            //CustomerVM customerVM = _customersDetailsService.FetchCustomerDetailsByName(requestedData.OrganizationName);
                            requestedData.CustomerID = customerVM.Id;
                            requestedData.OrganizationName = customerVM.Name;
                            createNewRequest.OrganizationName = customerVM.Name;
                            //var Attachment = message.Attachments;

                            //Get User /createor details by customer Email
                            UserVM userVM = _mapper.Map<Common.ViewModel.UserVM>(unityOfWork.CustomersDetailsRepository.FetchUserDetailsByEmail(RequesterUserEmail, _configuration));
                            if (userVM != null)
                            {
                                requestedData.Email = userVM.Email;
                            }
                            else
                            {
                                //userVM = null;
                                userVM = _mapper.Map<Common.ViewModel.UserVM>(unityOfWork.CustomersDetailsRepository.FetchUserDetailsByEmail(customerVM.Email, _configuration));
                                if (userVM != null)
                                {
                                    requestedData.Email = userVM.Email;
                                }
                                
                            }

                            //Get all Operation manager lists that recive the email
                            List<ViewExtendedUser> users = new List<ViewExtendedUser>();
                            var emaiList = new List<Guid>();
                            users = GetAllUserInfo(userVM.Id, userVM.Roles);

                            var operationManagerList = users.FindAll(u => u.AssociatedCustomers.Any(c => c.Id == customerVM.Id) && u.Role.Contains("Operations Manager") || u.Role.Contains("Admin")); // add customer Id and filter later
                            emaiList = operationManagerList.Select(u => u.Id).ToList();

                            BOS.StarterCode.Common.ViewModel.Created Creator = new Common.ViewModel.Created();
                            Creator.CreatedBy = userVM.Id;
                            Creator.DisplayName = userVM.Firstname + " " + userVM.Lastname;
                            Creator.ProfilePicture = userVM.ProfileImage ?? "";
                            Creator.UserName = userVM.Username;

                            requestedData.Creator = Creator;
                            RequestTypeEntityVM requestTypeEntityVM = FetchRequestDetailsByRequestType(requestedData.CurrentRequestType.ToLower());

                            if (requestedData.CurrentRequestType.ToLower() == "it" || requestedData.CurrentRequestType.ToLower() == "service")
                            {
                                var ticketName = "New - " + requestedData.Title;
                                var ticketRequest = new
                                {
                                    statusId = requestedData.StatusId,
                                    typeId = requestTypeEntityVM.Id,
                                    name = ticketName,
                                    createdDate = DateTime.Now.ToString("yyyy-MM-ddThh:mmZ"),
                                    customerID = requestedData.CustomerID,
                                    creator = requestedData.Creator
                                };
                                requestedData.TypeId = (Guid)requestTypeEntityVM.Id;

                                requestor.RequestedBy = requestedData.Creator.CreatedBy;
                                requestor.DisplayName = requestedData.Creator.DisplayName;
                                requestor.UserName = requestedData.Creator.UserName;

                                submitter.SubmittedBy = requestedData.Creator.CreatedBy;
                                submitter.DisplayName = requestedData.Creator.DisplayName;
                                submitter.UserName = requestedData.Creator.UserName;


                                createNewRequest.CustomerID = requestedData.CustomerID;
                                createNewRequest.DueDate = requestedData.DueDate;
                                createNewRequest.ProrityLevel = requestedData.ProrityLevel;
                                createNewRequest.Requestor = requestor;
                                createNewRequest.StatusId = newStatusId;
                                createNewRequest.SubmittedDate = DateTime.Now.ToString("yyyy-MM-ddThh:mmZ");
                                createNewRequest.Submitter = submitter;
                                createNewRequest.Title = requestedData.Title;
                                createNewRequest.TypeId = requestedData.TypeId;
                                //createNewRequest.OrganizationName = requestedData.OrganizationName;
                                if (attachments.Count > 0 && attachments != null)
                                {
                                    foreach (Attachment attachment in attachments)
                                    {
                                        Common.ViewModel.Attachments attachments1 = new Common.ViewModel.Attachments();

                                        attachments1.FileName = attachment.FileName;
                                        attachments1.FileSize = attachment.FileSize;
                                        attachments1.FileType = attachment.FileType;
                                        attachments1.FileUrl = attachment.FileUrl;

                                        createNewRequest.Attachments.Add(attachments1);
                                    }
                                }
                                createNewRequest.Details = "{}";
                                createNewRequest.Quantity = "";
                                createNewRequest.Price = "0";
                                createNewRequest.ProductUrl = "";
                                createNewRequest.SelectedItemProductUrl = "";
                                createNewRequest.ChildRequestTypeName = "";
                                createNewRequest.Notes = "";
                                createNewRequest.TrackStatus = "";
                                List<dynamic> requestList = new List<dynamic>()
                                {
                                    createNewRequest
                                };

                                var ticketResponse = await _ticketClient.CreateTicketAsync(ticketRequest);
                                if (!ticketResponse.IsSuccessStatusCode)
                                {
                                    throw new Exception("Error fetching Request Types: " + "\nResponse:" + JsonConvert.SerializeObject(ticketResponse.BOSErrors));
                                }
                                else
                                {
                                    createNewRequest.TicketId = ticketResponse.Ticket.Id;
                                    var requestResponse = await _ticketClient.CreateRequestAsync(createNewRequest);
                                    if (requestedData.Creator.UserName != null)
                                    {
                                        if (emaiList.Contains(requestedData.Creator.CreatedBy) == false)
                                            emaiList.Add(requestedData.Creator.CreatedBy);
                                        Ticket Ticket = ticketResponse.Ticket;
                                        Request Request = requestResponse.Request;
                                        Ticket.OrganizationName = requestedData.OrganizationName;

                                        //Send email request for confirm Service request 
                                        await SendNewRequestAddedEmail(emaiList.Distinct().ToList(), Ticket, Request, requestedData.Creator.CreatedBy, users, requestedData.TypeId);
                                    }
                                    var subject = "New Request " + ticketResponse.Ticket.Name + " Created by " + requestedData.Creator.UserName;
                                    var action = "New Request " + ticketResponse.Ticket.Name + " Created";
                                    var target = ticketResponse.Ticket.Id.ToString();

                                    //Request Formfields Saving
                                    var GetTicketResponse = await _ticketClient.GetTicketByIdAsync(ticketResponse.Ticket.Id, _configuration);
                                    Ticketlist = GetTicketResponse.Tickets;
                                    RequestsByTicket = Ticketlist.FirstOrDefault().Requests;
                                    Requestdetails = RequestsByTicket.FirstOrDefault();
                                    var operation = "New Request";
                                    await _activityClient.LogActivity(ticketResponse.Ticket.Id.ToString(), subject, target, action, operation, "Tickets");

                                }

                            }
                            // for supply request just save in database and display in list
                            else if (requestedData.CurrentRequestType.ToLower() == "supply")
                            {
                                UncategorizedRequest uncategorizedRequest = new UncategorizedRequest();

                                uncategorizedRequest.CustomerId = requestedData.CustomerID;
                                uncategorizedRequest.StatusId = requestedData.StatusId;
                                uncategorizedRequest.SubmittedDate = DateTime.Now.ToString("yyyy-MM-ddThh:mmZ");
                                uncategorizedRequest.CreatedBy = requestedData.Creator.CreatedBy;
                                uncategorizedRequest.DisplayName = requestedData.Creator.DisplayName;
                                uncategorizedRequest.UserName = requestedData.Creator.UserName;
                                uncategorizedRequest.Title = requestedData.Title;
                                uncategorizedRequest.TypeId = requestTypeEntityVM.Id;
                                uncategorizedRequest.CreatedDate = DateTime.Now.ToString("MMM dd, yyyy");
                                uncategorizedRequest.IsDeleted = false;
                                uncategorizedRequest.OrganizationId = requestedData.OrganizationName;
                                uncategorizedRequest.ModifiedDate = string.Empty;
                                //if (attachments.Count > 0 && attachments != null)
                                //{
                                //    uncategorizedRequest.Attachments.AddRange((IEnumerable<Common.ViewModel.Attachments>)attachments);
                                //}

                                unityOfWork.RequestDetailsRepository.SaveUncategorizedRequest(uncategorizedRequest, _configuration);

                                var subject = "New Uncategorized Request " + uncategorizedRequest.Title.ToString() + " Created by " + uncategorizedRequest.DisplayName.ToString();
                                var action = "New Uncategorized Request " + uncategorizedRequest.Title.ToString() + " Created";
                                var target = uncategorizedRequest.Title.ToString();
                                var operation = "New Request";

                                var res = await SendUncategorizedRequestEmail(emaiList.Distinct().ToList(), users, subject, uncategorizedRequest);

                                await _activityClient.LogActivity(uncategorizedRequest.Title.ToString(), subject, target, action, operation, "Tickets");

                            }
                        }
                    }
                    client.Inbox.AddFlags(uids, MessageFlags.Seen, true);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteLogFile("Error at Create request by email." + DateTime.Now.ToString(), ex);
                LogServiceActivity.WriteLogFile("Error at Create request by email.", ex, Convert.ToString(_webHostEnvironment.WebRootPath + "\\ErrorLog"));
            }

        }