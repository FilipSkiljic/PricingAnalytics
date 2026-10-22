/**
 * Values shared across the application, kept in one place so a change does not
 * have to be repeated in every service.
 */

/**
 * Address of the Web API. The application is meant to run locally, so this is the
 * development address; a deployment would read it from the build configuration.
 */
export const API_BASE_URL = 'http://localhost:5049';

/** Key under which the signed-in session is kept in the browser's local storage. */
export const SESSION_STORAGE_KEY = 'pricing-analytics.session';
