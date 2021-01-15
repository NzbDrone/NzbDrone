import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import SeriesTitleLink from 'Series/SeriesTitleLink';
import SeasonPassSeason from './SeasonPassSeason';
import { getSeriesStatusDetails } from 'Series/SeriesStatus';
import styles from './SeasonPassRow.css';

class SeasonPassRow extends Component {

  //
  // Render

  render() {
    const {
      seriesId,
      monitored,
      status,
      title,
      titleSlug,
      seriesType,
      network,
      qualityProfile,
      languageProfile,
      nextAiring,
      previousAiring,
      added,
      statistics,
      latestSeason,
      year,
      path,
      genres,
      ratings,
      certification,
      tags,
      useSceneNumbering,
      seasons,
      isSaving,
      isSelected,
      onSelectedChange,
      onSeriesMonitoredPress,
      onSeasonMonitoredPress
    } = this.props;

    const statusDetails = getSeriesStatusDetails(status);

    return (
      <TableRow>
        <TableSelectCell
          id={seriesId}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        <TableRowCell className={styles.status}>
          <Icon
            className={styles.statusIcon}
            name={statusDetails.icon}
            title={statusDetails.title}

          />
        </TableRowCell>

        <TableRowCell className={styles.title}>
          <SeriesTitleLink
            titleSlug={titleSlug}
            title={title}
          />
        </TableRowCell>

        <TableRowCell className={styles.monitored}>
          <MonitorToggleButton
            monitored={monitored}
            isSaving={isSaving}
            onPress={onSeriesMonitoredPress}
          />
        </TableRowCell>

        <TableRowCell className={styles.seasons}>
          {
            seasons.map((season) => {
              return (
                <SeasonPassSeason
                  key={season.seasonNumber}
                  {...season}
                  onSeasonMonitoredPress={onSeasonMonitoredPress}
                />
              );
            })
          }
        </TableRowCell>
      </TableRow>
    );
  }
}

SeasonPassRow.propTypes = {
  seriesId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  seriesType: PropTypes.string.isRequired,
  network: PropTypes.string,
  qualityProfile: PropTypes.object.isRequired,
  languageProfile: PropTypes.object.isRequired,
  nextAiring: PropTypes.string,
  previousAiring: PropTypes.string,
  added: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  latestSeason: PropTypes.object,
  year: PropTypes.number,
  path: PropTypes.string.isRequired,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  ratings: PropTypes.object.isRequired,
  certification: PropTypes.string,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  useSceneNumbering: PropTypes.bool.isRequired,
  seasons: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onSeriesMonitoredPress: PropTypes.func.isRequired,
  onSeasonMonitoredPress: PropTypes.func.isRequired
};

SeasonPassRow.defaultProps = {
  isSaving: false
};

export default SeasonPassRow;
