import { test as setup } from '@playwright/test';
import { ConstantHelper, UiHelpers } from '@umbraco-cms/acceptance-test-helpers';
import { STORAGE_STATE } from '../../playwright.config';

setup('authenticate', async ({ page }) => {
    const umbracoUi = new UiHelpers(page);

    await umbracoUi.goToBackOffice();
    await umbracoUi.login.enterEmail(process.env.UMBRACO_USER_LOGIN!);
    await umbracoUi.login.enterPassword(process.env.UMBRACO_USER_PASSWORD!);
    await umbracoUi.login.clickLoginButton();
    await umbracoUi.content.goToSection(ConstantHelper.sections.content);

    await page.context().storageState({ path: STORAGE_STATE });
});
