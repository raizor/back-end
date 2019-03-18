﻿using Microsoft.AspNetCore.Mvc;
using MockQueryable.Moq;
using Moq;
using PolloPollo.Repository;
using PolloPollo.Shared;
using PolloPollo.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PolloPollo.Web.Tests.Controllers
{
    public class ReceiversControllerTests
    {
        [Fact]
        public async Task GetReturnsDTOs()
        {
            var dto = new ReceiverDTO();
            var all = new[] { dto }.AsQueryable().BuildMock();
            var repository = new Mock<IReceiverRepository>();
            repository.Setup(s => s.Read()).Returns(all.Object);

            var controller = new ReceiversController(repository.Object);

            var result = await controller.Get();

            Assert.Equal(dto, result.Value.Single());
        }

        [Fact]
        public async Task GetGivenExistingIdReturnsDto()
        {
            var dto = new ReceiverDTO();
            var repository = new Mock<IReceiverRepository>();
            repository.Setup(s => s.FindAsync(42)).ReturnsAsync(dto);

            var controller = new ReceiversController(repository.Object);

            var get = await controller.Get(42);

            Assert.Equal(dto, get.Value);
        }

        [Fact]
        public async Task GetGivenNonExistingIdReturnsNotFound()
        {
            var repository = new Mock<IReceiverRepository>();

            var controller = new ReceiversController(repository.Object);

            var get = await controller.Get(42);

            Assert.IsType<NotFoundResult>(get.Result);
        }


        [Fact]
        public async Task PutGivenDtoUpdatesEntity()
        {
            var repository = new Mock<IReceiverRepository>();

            var controller = new ReceiversController(repository.Object);

            var dto = new UserUpdateDTO();

            await controller.Put(42, dto);

            repository.Verify(s => s.UpdateAsync(dto));
        }

        [Fact]
        public async Task PutReturnsNoContent()
        {
            var dto = new UserUpdateDTO();
            var repository = new Mock<IReceiverRepository>();
            repository.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(true);
            var controller = new ReceiversController(repository.Object);

            var put = await controller.Put(42, dto);

            Assert.IsType<NoContentResult>(put);
        }

        [Fact]
        public async Task PutGivenRepositoryReturnsFalseReturnsNotFound()
        {
            var repository = new Mock<IReceiverRepository>();

            var controller = new ReceiversController(repository.Object);

            var dto = new UserUpdateDTO();

            var put = await controller.Put(42, dto);

            Assert.IsType<NotFoundResult>(put);
        }

        [Fact]
        public async Task DeleteGivenExistingIdDeletesEntity()
        {
            var repository = new Mock<IReceiverRepository>();

            var controller = new ReceiversController(repository.Object);

            await controller.Delete(42);

            repository.Verify(s => s.DeleteAsync(42));
        }

        [Fact]
        public async Task DeleteReturnsNoContent()
        {
            var repository = new Mock<IReceiverRepository>();

            repository.Setup(s => s.DeleteAsync(42)).ReturnsAsync(true);

            var controller = new ReceiversController(repository.Object);

            var delete = await controller.Delete(42);

            Assert.IsType<NoContentResult>(delete);
        }

        [Fact]
        public async Task DeleteGivenNonExistingIdReturnsNotFound()
        {
            var repository = new Mock<IReceiverRepository>();

            var controller = new ReceiversController(repository.Object);

            var delete = await controller.Delete(42);

            Assert.IsType<NotFoundResult>(delete);
        }
    }
}