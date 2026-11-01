import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
const catalogue = [
  {id:"ocean",slug:"ocean",title:"Ocean life",category:"Natural world",summary:"Discover ocean life.",description:"Explore the depths of the sea.",gallery:"Gallery 1",minutes:30,colour:"ocean",access:["Step-free access"]},
  {id:"light",slug:"light",title:"Light and colour",category:"Art & design",summary:"See light differently.",description:"Explore colour and perception.",gallery:"Gallery 2",minutes:20,colour:"light",access:["Seating available"]},
  {id:"earth",slug:"earth",title:"Our earth",category:"Science",summary:"Explore the planet.",description:"Discover the earth beneath us.",gallery:"Gallery 3",minutes:25,colour:"earth",access:["Step-free access"]},
];
test.beforeEach(async ({page}) => {
  await page.route("**/api/exhibitions", route => route.fulfill({json:catalogue}));
});
test("kiosk never restores or saves visitor tokens; finish clears state", async ({page}) => {
  await page.addInitScript(() => localStorage.setItem("museum-explorer-itinerary", '{"id":"old","accessToken":"secret"}'));
  const requests:string[] = [];
  page.on("request", request => { if(request.url().includes("/api/itineraries")) requests.push(request.url()); });
  await page.goto("/kiosk");
  await page.getByRole("button", {name:"+ Add to your visit : Ocean life", exact:true}).click();
  await expect(page.getByRole("button", {name:"Save my itinerary"})).toHaveCount(0);
  expect(await page.evaluate(() => localStorage.getItem("museum-explorer-itinerary"))).toBeNull();
  await page.getByRole("button", {name:"Finish visit",exact:true}).first().click();
  await expect(page.getByText("Start with a spark.")).toBeVisible();
  await expect(page.locator("#main")).toBeFocused();
  expect(requests).toEqual([]);
});
test("idle dialog traps focus, allows extension, then resets", async ({page}) => {
  await page.clock.install();
  await page.goto("/kiosk");
  const add = page.getByRole("button", {name:"+ Add to your visit : Ocean life",exact:true});
  await add.click();
  await page.clock.runFor(240_000);
  await expect(page.getByRole("dialog")).toBeVisible();
  await expect(page.getByRole("button", {name:"Continue my visit"})).toBeFocused();
  await page.keyboard.press("Shift+Tab");
  await expect(page.getByRole("dialog").getByRole("button", {name:"Finish visit"})).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).not.toBeVisible();
  await expect(page.getByRole("button", {name:"✓ Added to your visit : Ocean life",exact:true})).toBeFocused();
  await page.clock.runFor(300_000);
  await expect(page.getByText("Start with a spark.")).toBeVisible();
  await expect(page.locator("#main")).toBeFocused();
});
test("late resume clears stale visit", async ({page}) => {
  await page.clock.install();
  await page.goto("/kiosk");
  await page.getByRole("button", {name:"+ Add to your visit : Ocean life",exact:true}).click();
  await page.clock.fastForward(360_000);
  await expect(page.getByText("Start with a spark.")).toBeVisible();
});
test("personal mode retains saving and restore", async ({page}) => {
  await page.route("**/api/itineraries", route => route.fulfill({json:{id:"saved",accessToken:"test-token"}}));
  await page.route("**/api/itineraries/saved", route => route.fulfill({json:{exhibitionIds:["ocean"]}}));
  await page.goto("/");
  await page.getByRole("button", {name:"+ Add to your visit : Ocean life",exact:true}).click();
  await page.getByRole("button", {name:"Save my itinerary"}).click();
  await expect(page.locator(".status")).toContainText("Saved to the database.");
  await page.reload();
  await expect(page.locator(".status")).toContainText("Your saved itinerary has been restored.");
});
for (const width of [1280, 320]) {
  test(`accessibility and reflow at ${width}px`, async ({page}) => {
    await page.setViewportSize({width,height:900});
    await page.goto("/kiosk");
    await page.getByRole("button", {name:"Look closer : Ocean life"}).click();
    await page.getByRole("button", {name:"+ Add to your visit : Ocean life",exact:true}).click();
    await page.getByRole("button", {name:"Remove Ocean life",exact:true}).click();
    await expect(page.locator("#visit-title")).toBeFocused();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    const results = await new AxeBuilder({page}).withTags(["wcag2a","wcag2aa","wcag21aa","wcag22aa"]).analyze();
    await page.screenshot({ path: `test-results/kiosk-${width}.png`, fullPage: true });
    expect(results.violations).toEqual([]);
  });
}
test("warning dialog accessibility", async ({page}) => {
  await page.clock.install();
  await page.goto("/kiosk");
  await expect(page.getByRole("button", {name:"+ Add to your visit : Ocean life",exact:true})).toBeVisible();
  await page.clock.runFor(240_000);
  await expect(page.getByRole("dialog")).toBeVisible();
  expect((await new AxeBuilder({page}).withTags(["wcag2a","wcag2aa","wcag21aa","wcag22aa"]).analyze()).violations).toEqual([]);
});

test("keyboard skip link and reduced motion", async ({page}) => {
  await page.emulateMedia({reducedMotion:"reduce"});
  await page.goto("/kiosk");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("link", {name:"Skip to content"})).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.locator("#main")).toBeFocused();
  expect(await page.evaluate(() => getComputedStyle(document.documentElement).scrollBehavior)).toBe("auto");
});
