//fetch shopify products

 public void SaveProductDetails()
        {
            try
            {
                var httpClient1 = new HttpClient
                {
                    BaseAddress = new Uri("Base address Uri to fetch shopify data"),
                    DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Basic",
                                            Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["Shopify:Username"] + ":" + _configuration["Shopify:Password"]))) }
                };

                var taskPostResponse3 = httpClient1.GetAsync($"products/count.json").Result;
                var result3 = taskPostResponse3.Content.ReadAsStringAsync().Result;

                dynamic productCount = JsonConvert.DeserializeObject<ExpandoObject>(result3);
                List<ProductDetailsVM> productsVM = new List<ProductDetailsVM>();
                string page_info = string.Empty;

                while (productsVM.Count < productCount.count)
                {
                    string productUrl;
                    if (!string.IsNullOrWhiteSpace(page_info))
                        productUrl = $"products.json?limit=250&page_info={page_info}&fields=id,title,images";
                    else
                        productUrl = $"products.json?limit=250&fields=id,title,images";

                    var taskPostResponse2 = httpClient1.GetAsync(productUrl).Result;
                    if (taskPostResponse2.IsSuccessStatusCode)
                    {
                        var result2 = taskPostResponse2.Content.ReadAsStringAsync().Result;
                        var allProducts = JsonConvert.DeserializeObject<ShopifyProductResponseVM>(result2);
                        var isHeaderHasLink = taskPostResponse2.Headers.TryGetValues("Link", out IEnumerable<string> Headers);
                        if (isHeaderHasLink)
                        {
                            var nextPageURL = Headers.FirstOrDefault().Split(",").Length == 1 ? Headers.FirstOrDefault().Split(",")[0].Replace("<", "")
                                                                                                         .Replace(">", "").Split("; rel")[0]
                                                                                                        : Headers.FirstOrDefault().Split(",")[1].Replace("<", "")
                                                                                                         .Replace(">", "").Split("; rel")[0];
                            page_info = HttpUtility.ParseQueryString(new Uri(nextPageURL).Query)["page_info"];
                            foreach (var item in allProducts.products)
                            {

                                productsVM.Add(new ProductDetailsVM()
                                {
                                    ProductId = item.id.ToString(),
                                    ProductTitle = item.title ?? "",
                                    UpdatedDateTime = DateTime.UtcNow,
                                    ImageSrc = item.image ?? ""
                                });
                            }
                        }
                    }

                }

                IUnityOfWork unityOfWork = new UnityOfWork(_context);
                unityOfWork.ProductDetailsRepository.DeleteAllProductDetails();
                unityOfWork.ProductDetailsRepository.SaveProductDetails(_mapper.Map<List<ProductDetails>>(productsVM));
                //unityOfWork.Save();

                //List<ProductDetails> products = new List<ProductDetails>();
                //int i=0;
                //while (true)
                //{
                //    products = _mapper.Map<List<ProductDetails>>(productsVM).Skip(i).Take(1000).ToList();
                //    IUnityOfWork unityOfWork = new UnityOfWork(_context);
                //    unityOfWork.ProductDetailsRepository.SaveProductDetails(products);
                //    unityOfWork.Save();
                //    i += 1000;
                //    if (i > productsVM.Count)
                //        break;
                //}

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ErrorLog.WriteLogFile("Error occured in Product details service() ", ex);
                //LogServiceActivity.WriteLogFile("Error occured in AddActivityLogAsync() of RequestDetaols service.", ex);
            }
            finally
            {
            }
        }