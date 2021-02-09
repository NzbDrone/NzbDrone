import React from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import LoadingMessage from 'Components/Loading/LoadingMessage';
import styles from './LoadingPage.css';

function LoadingPage() {
  return (
    <div className={styles.page}>
      <img src="../../Logo/Sonarr.svg" className={styles.logoFull}></img>
      <LoadingMessage />
      <LoadingIndicator />
    </div>
  );
}

export default LoadingPage;
