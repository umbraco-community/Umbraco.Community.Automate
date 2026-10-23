// Calls DevToController as the signed-in backoffice user. Resolves to { data } or { error };
// callers show errors themselves, so the backoffice's own error notifications are off.
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { tryExecute } from "@umbraco-cms/backoffice/resources";

export const devToApi = (host, method, path, options = {}) =>
    tryExecute(
        host,
        umbHttpClient[method]({
            url: `/umbraco/management/api/v1/devto/${path}`,
            ...options,
            security: [{ scheme: "bearer", type: "http" }],
            throwOnError: true,
        }),
        { disableNotifications: true },
    );
