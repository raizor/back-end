﻿using System;
using System.Threading.Tasks;
using PolloPollo.Entities;
using PolloPollo.Shared;
using PolloPollo.Shared.DTO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PolloPollo.Services.Utils;
using MimeKit;
using MailKit.Net.Smtp;

namespace PolloPollo.Services
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly PolloPolloContext _context;

        public ApplicationRepository(PolloPolloContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create application from ApplicationCreateDTO and return an ApplicationDTO
        /// </summary>
        public async Task<ApplicationDTO> CreateAsync(ApplicationCreateDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            var application = new Application
            {
                UserId = dto.UserId,
                ProductId = dto.ProductId,
                Motivation = dto.Motivation,
                Created = DateTime.UtcNow,
                Status = ApplicationStatusEnum.Open
            };

            try
            {
                var createdApplication = _context.Applications.Add(application);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return null;
            }

            var receiver = (from u in _context.Users
                            where u.Id == application.UserId
                            select new
                            {
                                ReceiverName = u.FirstName + " " + u.SurName,
                                u.Country,
                                u.Thumbnail
                            }).FirstOrDefault();

            var product = (from p in _context.Products
                           where p.Id == application.ProductId
                           select new
                           {
                               ProductId = p.Id,
                               ProductTitle = p.Title,
                               ProductPrice = p.Price,
                               ProducerId = p.UserId
                           }).FirstOrDefault();

            var applicationDTO = new ApplicationDTO
            {
                ApplicationId = application.Id,
                ReceiverId = application.UserId,
                ReceiverName = receiver.ReceiverName,
                Country = receiver.Country,
                Thumbnail = receiver.Thumbnail,
                ProductId = product.ProductId,
                ProductTitle = product.ProductTitle,
                ProductPrice = product.ProductPrice,
                ProducerId = product.ProducerId,
                Motivation = application.Motivation,
                Status = application.Status,
            };

            return applicationDTO;
        }

        /// <summary>
        /// Find an application by id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns name="ApplicationDTO"></returns>
        public async Task<ApplicationDTO> FindAsync(int applicationId)
        {
            var application = await (from a in _context.Applications
                                     where a.Id == applicationId
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
                                         DonationDate = a.DonationDate,
                                     }).SingleOrDefaultAsync();

            if (application == null)
            {
                return null;
            }

            return application;
        }

        /// <summary>
        /// Update state of application
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<(bool, bool)> UpdateAsync(ApplicationUpdateDTO dto)
        {
            var application = await _context.Applications.
                FirstOrDefaultAsync(p => p.Id == dto.ApplicationId);

            if (application == null)
            {
                return (false, false);
            }

            application.Status = dto.Status;
            application.LastModified = DateTime.UtcNow;

            var mailSent = false;
            if (dto.Status == ApplicationStatusEnum.Pending)
            {
                application.DonationDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
#if DEBUG
                mailSent = true;
#else
                var ProducerId = await (from a in _context.Applications
                                      where a.Id == dto.ApplicationId
                                      select new
                                      {
                                          Id = a.Product.UserId
                                      }).SingleOrDefaultAsync();
                var Producer = await _context.Producers.
                    FirstOrDefaultAsync(p => p.Id == ProducerId.Id);
                var producerAddress = Producer.Zipcode != null 
                                        ? Producer.Street + " " + Producer.StreetNumber + ", " + Producer.Zipcode + " " + Producer.City
                                        : Producer.Street + " " + Producer.StreetNumber + ", " + Producer.City;
                mailSent = SendConfirmationEmail(application.User.Email, application.Product.Title, producerAddress);
#endif
            }
            else if (dto.Status == ApplicationStatusEnum.Open)
            {
                application.DonationDate = null;
            }

            await _context.SaveChangesAsync();

            return (true, mailSent);
        }

        public bool SendConfirmationEmail(string ReceiverEmail, string ProductName, string ProducerAddress)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("no-reply@pollopollo.org"));
            message.To.Add(new MailboxAddress(ReceiverEmail));
            message.Subject = "Your PolloPollo application received a donation";
            message.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = $"Your application for {ProductName} has been fulfilled by a donor. The product can now be picked up at {ProducerAddress}. When you receive your product, you must log on to the PolloPollo website and confirm reception of the product. When you confirm reception, the donated funds are released to the Producer of the product."
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    //client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect("localhost", 25, false);

                    // Note: only needed if the SMTP server requires authentication
                    //   client.Authenticate("joey", "password");

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

        /// <summary>
        /// Retrieve all open applications
        /// </summary>
        /// <returns></returns>
        public IQueryable<ApplicationDTO> ReadOpen()
        {
            var entities = from a in _context.Applications
                           where a.Status == ApplicationStatusEnum.Open
                           orderby a.Created descending
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
                           };

            return entities;
        }

        /// <summary>
        /// Retrieve all applications by specified receiver
        /// </summary>
        /// <param name="receiverId"></param>
        /// <returns></returns>
        public IQueryable<ApplicationDTO> Read(int receiverId)
        {
            var entities = from a in _context.Applications
                           where a.UserId == receiverId
                           orderby a.Created descending
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
                               DonationDate = a.DonationDate,
                           };

            return entities;
        }

        public async Task<ContractInformationDTO> GetContractInformationAsync(int applicationId)
        {
            var application = await (from a in _context.Applications
                                     where a.Id == applicationId
                                     select new
                                     {
                                         a.Product.Price,
                                         a.Product.UserId
                                     }).FirstOrDefaultAsync();
            if (application == null)
            {
                return null;
            }

            var producer = await (from p in _context.Producers
                                  where p.UserId == application.UserId
                                  select new
                                  {
                                      p.DeviceAddress,
                                      p.WalletAddress
                                  }).FirstOrDefaultAsync();

            return new ContractInformationDTO
            {
                ProducerDevice = producer.DeviceAddress,
                ProducerWallet = producer.WalletAddress,
                Price = application.Price
            };
        }

        /// <summary>
        /// Delete an application by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns name="bool"></returns>
        public async Task<bool> DeleteAsync(int userId, int id)
        {
            var application = _context.Applications.Find(id);

            if (application == null)
            {
                return false;
            }

            if (userId != application.UserId)
            {
                return false;
            }

            if (application.Status != ApplicationStatusEnum.Open)
            {
                return false;
            }

            _context.Applications.Remove(application);

            await _context.SaveChangesAsync();

            return true;
        }

    }
}
