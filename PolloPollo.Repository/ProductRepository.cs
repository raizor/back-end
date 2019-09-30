﻿using PolloPollo.Entities;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PolloPollo.Services.Utils;
using PolloPollo.Shared.DTO;
using PolloPollo.Shared;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
namespace PolloPollo.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly PolloPolloContext _context;
        private readonly IImageWriter _imageWriter;

        public ProductRepository(IImageWriter imageWriter, PolloPolloContext context)
        {
            _imageWriter = imageWriter;
            _context = context;
        }

        /// <summary>
        /// Create product from ProductCreateDTO and return a ProductDTO
        /// </summary>
        public async Task<(ProductDTO created, string message)> CreateAsync(ProductCreateDTO dto)
        {
            if (dto == null)
            {
                return (null, "Empty DTO");
            }

            var producerUser = await (from p in _context.Users
                    where p.Id == dto.UserId
                    select new {
                        p.Producer.WalletAddress
                    }).FirstOrDefaultAsync();

            if (producerUser == null)
            {
                return (null, "Producer not found");
            }
            else if (string.IsNullOrEmpty(producerUser.WalletAddress))
            {
                return (null, "No wallet address");
            }

            var product = new Product
            {
                Title = dto.Title,
                UserId = dto.UserId,
                Price = dto.Price,
                Description = dto.Description,
                Country = dto.Country,
                Location = dto.Location,
                Available = true,
                Rank = dto.Rank,
                Created = DateTime.UtcNow
            };

            try
            {
                var createdProduct = _context.Products.Add(product);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return (null, "Error");
            }

            var productDTO = new ProductDTO
            {
                ProductId = product.Id,
                Title = dto.Title,
                UserId = dto.UserId,
                Price = dto.Price,
                Country = dto.Country,
                Description = dto.Description,
                Location = dto.Location,
                Available = product.Available,
                Rank = dto.Rank,
            };

            return (productDTO, "Created");
        }

        /// <summary>
        /// Find a product by id
        /// </summary>
        /// <param name="productId"></param>
        /// <returns name="ProductDTO"></returns>
        public async Task<ProductDTO> FindAsync(int productId)
        {
            var product = await (from p in _context.Products
                                 where p.Id == productId
                                 select new ProductDTO
                                 {
                                     ProductId = p.Id,
                                     Title = p.Title,
                                     UserId = p.UserId,
                                     Price = p.Price,
                                     Description = p.Description,
                                     Country = p.Country,
                                     Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(p.Thumbnail),
                                     Location = p.Location,
                                     Available = p.Available,
                                     Rank = p.Rank,
                                     OpenApplications =
                                        from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Open
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        },
                                    PendingApplications =
                                        from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Pending
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        },
                                     ClosedApplications =
                                             from a in p.Applications
                                             where a.Status == ApplicationStatusEnum.Unavailable
                                             select new ApplicationDTO
                                             {
                                                 ApplicationId = a.Id,
                                                 ReceiverId = a.UserId,
                                                 ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                                 Country = a.User.Country,
                                                 Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                                 ProductId = a.Product.Id,
                                                 ProductTitle = a.Product.Title,
                                                 ProductPrice = a.Product.Price,
                                                 ProducerId = a.Product.UserId,
                                                 Motivation = a.Motivation,
                                                 Status = a.Status,
                                             },
                                 }).SingleOrDefaultAsync();

            if (product == null)
            {
                return null;
            }

            return product;
        }

        /// <summary>
        /// Retrieve all products
        /// </summary>
        /// <returns></returns>
        public IQueryable<ProductDTO> ReadOpen()
        {
            var entities = from p in _context.Products
                           where p.Available == true
                           orderby p.Rank descending
                           orderby p.Created descending
                           select new ProductDTO
                           {
                               ProductId = p.Id,
                               Title = p.Title,
                               UserId = p.UserId,
                               Price = p.Price,
                               Country = p.Country,
                               Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(p.Thumbnail),
                               Description = p.Description,
                               Location = p.Location,
                               Available = p.Available,
                               Rank = p.Rank,
                               OpenApplications =
                                        (from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Open
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        }).ToList(),
                               PendingApplications =
                                        (from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Pending
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        }).ToList(),
                               ClosedApplications =
                                        (from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Unavailable
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        }).ToList(),
                           };

            return entities;
        }

        public async Task<(bool status, int pendingApplications, bool emailSent)> UpdateAsync(ProductUpdateDTO dto)
        {
            var pendingApplications = 0;
            var sent = false;

            var product = await _context.Products.
                Include(p => p.Applications).
                FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (product == null)
            {
                return (false, pendingApplications, sent);
            }

            foreach (var application in product.Applications)
            {
                if (application.Status == ApplicationStatusEnum.Open && !dto.Available)
                {
                    application.Status = ApplicationStatusEnum.Unavailable;
                    await _context.SaveChangesAsync();

#if !DEBUG
                    // Send email to receiver informing them that their application has been cancelled
                    var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == application.UserId);
                    var receiverEmail = receiver.Email;
                    var productName = product.Title;
                    sent = SendEmail(receiverEmail, productName);
#endif

                }
                else if (application.Status == ApplicationStatusEnum.Pending)
                {
                    pendingApplications++;
                }
            }

            product.Available = dto.Available;

            await _context.SaveChangesAsync();

            return (true, pendingApplications, sent);
        }

        /// <summary>
        /// Send email about cancelled application to receiver
        /// </summary>
        /// <param></param>
        /// <returns></returns>
        public bool SendEmail(string ReceiverEmail, string ProductName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("no-reply@pollopollo.org"));
            message.To.Add(new MailboxAddress(ReceiverEmail));
            message.Subject = "PolloPollo application cancelled";
            message.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = $"You had an open application for {ProductName} but the Producer has removed the product from the PolloPollo platform, and your application for it has therefore been cancelled.You may log on to the PolloPollo platform to see if the product has been replaced by another product, you want to apply for instead.\n\nSincerely,\nThe PolloPollo Project"
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect("localhost", 25, false);
                    client.Send(message);
                    client.Disconnect(true);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> UpdateImageAsync(int id, IFormFile image)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return null;
            }

            var folder = ImageFolderEnum.@static.ToString();

            var oldThumbnail = product.Thumbnail;

            try
            {
                var fileName = await _imageWriter.UploadImageAsync(folder, image);

                product.Thumbnail = fileName;

                await _context.SaveChangesAsync();

                // Remove old image
                if (oldThumbnail != null)
                {
                    _imageWriter.DeleteImage(folder, oldThumbnail);
                }

                return ImageHelper.GetRelativeStaticFolderImagePath(fileName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        /// <summary>
        /// Retrieve all products by specified producer
        /// </summary>
        /// <param name="producerId"></param>
        /// <returns></returns>
        public IQueryable<ProductDTO> Read(int producerId)
        {
            var entities = from p in _context.Products
                           where p.UserId == producerId
                           orderby p.Rank descending
                           orderby p.Created descending
                           select new ProductDTO
                           {
                               ProductId = p.Id,
                               Title = p.Title,
                               UserId = p.UserId,
                               Price = p.Price,
                               Country = p.Country,
                               Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(p.Thumbnail),
                               Description = p.Description,
                               Location = p.Location,
                               Available = p.Available,
                               Rank = p.Rank,
                               OpenApplications =
                                        from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Open
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        },
                               PendingApplications =
                                        from a in p.Applications
                                        where a.Status == ApplicationStatusEnum.Pending
                                        select new ApplicationDTO
                                        {
                                            ApplicationId = a.Id,
                                            ReceiverId = a.UserId,
                                            ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                            Country = a.User.Country,
                                            Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                            ProductId = a.Product.Id,
                                            ProductTitle = a.Product.Title,
                                            ProductPrice = a.Product.Price,
                                            ProducerId = a.Product.UserId,
                                            Motivation = a.Motivation,
                                            Status = a.Status,
                                        },
                               ClosedApplications =
                                             from a in p.Applications
                                             where a.Status == ApplicationStatusEnum.Unavailable
                                             select new ApplicationDTO
                                             {
                                                 ApplicationId = a.Id,
                                                 ReceiverId = a.UserId,
                                                 ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                                 Country = a.User.Country,
                                                 Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                                 ProductId = a.Product.Id,
                                                 ProductTitle = a.Product.Title,
                                                 ProductPrice = a.Product.Price,
                                                 ProducerId = a.Product.UserId,
                                                 Motivation = a.Motivation,
                                                 Status = a.Status,
                                             },
                           };

            return entities;
        }

        /// <summary>
        /// Retrieve count of product
        /// </summary>
        public async Task<int> GetCountAsync()
        {
            return await _context.Products.CountAsync();
        }


    }
}
