import $ from 'jquery';

function getTranslations() {
  let localization = null;
  const ajaxOptions = {
    async: false,
    type: 'GET',
    global: false,
    dataType: 'json',
    url: `${window.Sonarr.apiRoot}/localization`,
    success: function(data) {
      localization = data;
    }
  };

  ajaxOptions.headers = ajaxOptions.headers || {};
  ajaxOptions.headers['X-Api-Key'] = window.Sonarr.apiKey;

  $.ajax(ajaxOptions);
  return localization;
}

const translations = getTranslations();

export default function translate(key, args = '') {
  if (args) {
    const translatedKey = translate(key);
    return translatedKey.replace(/\{(\w+)\}/g, (match, index) => {
      return args[index];
    });
  }

  return translations[key] || key;
}
