﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.HtmlCodec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class CustomPageService : MoongladeService
    {
        private readonly IHtmlCodec _htmlCodec;
        private readonly IRepository<CustomPageEntity> _customPageRepository;

        public CustomPageService(
            ILogger<CustomPageService> logger,
            IOptions<AppSettings> settings,
            IRepository<CustomPageEntity> customPageRepository,
            IHtmlCodec htmlCodec) : base(logger, settings)
        {
            _customPageRepository = customPageRepository;
            _htmlCodec = htmlCodec;
        }

        public Task<Response<CustomPage>> GetPageAsync(Guid pageId)
        {
            return TryExecuteAsync<CustomPage>(async () =>
            {
                var entity = await _customPageRepository.GetAsync(pageId);
                var item = EntityToCustomPage(entity);
                return new SuccessResponse<CustomPage>(item);
            });
        }

        public Task<Response<CustomPage>> GetPageAsync(string routeName)
        {
            return TryExecuteAsync<CustomPage>(async () =>
            {
                if (string.IsNullOrWhiteSpace(routeName))
                {
                    throw new ArgumentNullException(nameof(routeName));
                }

                var loweredRouteName = routeName.ToLower();
                var entity = await _customPageRepository.GetAsync(p => p.RouteName == loweredRouteName);
                var item = EntityToCustomPage(entity);
                return new SuccessResponse<CustomPage>(item);
            });
        }

        public Task<Response<IReadOnlyList<CustomPageMetaData>>> GetPagesMetaDataListAsync()
        {
            return TryExecuteAsync<IReadOnlyList<CustomPageMetaData>>(async () =>
            {
                var list = await _customPageRepository.SelectAsync(page => new CustomPageMetaData
                {
                    Id = page.Id,
                    CreateOnUtc = page.CreateOnUtc,
                    RouteName = page.RouteName,
                    Title = page.Title
                });

                return new SuccessResponse<IReadOnlyList<CustomPageMetaData>>(list);
            });
        }

        public Task<Response<Guid>> CreatePageAsync(CreateCustomPageRequest request)
        {
            return TryExecuteAsync<Guid>(async () =>
            {
                var uid = Guid.NewGuid();
                var customPage = new CustomPageEntity
                {
                    Id = uid,
                    Title = request.Title.Trim(),
                    RouteName = request.RouteName.ToLower().Trim(),
                    CreateOnUtc = DateTime.UtcNow,
                    HtmlContent = _htmlCodec.HtmlEncode(request.HtmlContent),
                    CssContent = request.CssContent,
                    HideSidebar = request.HideSidebar
                };

                await _customPageRepository.AddAsync(customPage);
                return new SuccessResponse<Guid>(uid);
            });
        }

        public Task<Response<Guid>> EditPageAsync(EditCustomPageRequest request)
        {
            return TryExecuteAsync<Guid>(async () =>
            {
                var page = await _customPageRepository.GetAsync(request.Id);
                if (null == page)
                {
                    throw new InvalidOperationException($"CustomPageEntity with Id '{request.Id}' is not found.");
                }

                page.Title = request.Title.Trim();
                page.RouteName = request.RouteName.ToLower().Trim();
                page.HtmlContent = _htmlCodec.HtmlEncode(request.HtmlContent);
                page.CssContent = request.CssContent;
                page.HideSidebar = request.HideSidebar;
                page.UpdatedOnUtc = DateTime.UtcNow;

                await _customPageRepository.UpdateAsync(page);
                return new SuccessResponse<Guid>(page.Id);
            });
        }

        public Response DeletePage(Guid pageId)
        {
            return TryExecute(() =>
            {
                var page = _customPageRepository.Get(pageId);
                if (null == page)
                {
                    throw new InvalidOperationException($"CustomPageEntity with Id '{pageId}' is not found.");
                }

                _customPageRepository.Delete(pageId);
                return new SuccessResponse();
            });
        }

        private CustomPage EntityToCustomPage(CustomPageEntity entity)
        {
            if (null == entity)
            {
                return null;
            }

            return new CustomPage
            {
                Id = entity.Id,
                Title = entity.Title.Trim(),
                CreateOnUtc = entity.CreateOnUtc,
                CssContent = entity.CssContent,
                RawHtmlContent = _htmlCodec.HtmlDecode(entity.HtmlContent),
                HideSidebar = entity.HideSidebar,
                RouteName = entity.RouteName.Trim().ToLower(),
                UpdatedOnUtc = entity.UpdatedOnUtc
            };
        }
    }
}
