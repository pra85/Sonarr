﻿using System.Collections.Generic;
using Sonarr.Http.REST;
using NzbDrone.Core.MediaFiles;
using Sonarr.Http;

namespace NzbDrone.Api.Episodes
{
    public class RenameEpisodeModule : SonarrRestModule<RenameEpisodeResource>
    {
        private readonly IRenameEpisodeFileService _renameEpisodeFileService;

        public RenameEpisodeModule(IRenameEpisodeFileService renameEpisodeFileService)
            : base("rename")
        {
            _renameEpisodeFileService = renameEpisodeFileService;

            GetResourceAll = GetEpisodes;
        }

        private List<RenameEpisodeResource> GetEpisodes()
        {
            int seriesId;

            if (Request.Query.SeriesId.HasValue)
            {
                seriesId = (int)Request.Query.SeriesId;
            }

            else
            {
                throw new BadRequestException("seriesId is missing");
            }

            if (Request.Query.SeasonNumber.HasValue)
            {
                var seasonNumber = (int)Request.Query.SeasonNumber;
                return ToListResource(() => _renameEpisodeFileService.GetRenamePreviews(seriesId, seasonNumber));
            }

            return ToListResource(() => _renameEpisodeFileService.GetRenamePreviews(seriesId));
        }
    }
}
