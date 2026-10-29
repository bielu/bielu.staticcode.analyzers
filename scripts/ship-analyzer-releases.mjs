// Marks the tracked analyzer rules as shipped: moves the body of each src/*/AnalyzerReleases.Unshipped.md
// into a "## Release x.y.z" section of the AnalyzerReleases.Shipped.md next to it, which is what the
// Microsoft.CodeAnalysis.Analyzers release tracking rules (RS2000-RS2008) expect once a version carrying
// those rules has been released.
//
// Run automatically by `npm run version` (i.e. right after `changeset version`), so the move lands in
// the "Version Packages" PR together with the version bump it belongs to. Idempotent: re-running with
// nothing unshipped is a no-op.
//
// Only a stable version ships a rule. A prerelease (changesets pre mode: 1.2.0-preview.0) leaves the
// files alone, because a rule that only appeared in a preview may still change before the release.
import { readFileSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, relative } from "node:path";

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const srcDir = join(repoRoot, "src");
const nugetPkgDir = join(repoRoot, "build", "changeset", "nuget-suite");

const version = JSON.parse(readFileSync(join(nugetPkgDir, "package.json"), "utf8")).version;
if (version.includes("-")) {
  console.log(`${version} is a prerelease; leaving the analyzer release files unshipped.`);
  process.exit(0);
}

// Splits a release tracking file into its leading "; comment" header and the rest, remembering the
// line ending so the file is written back the same way.
function read(path) {
  const text = readFileSync(path, "utf8");
  const eol = text.includes("\r\n") ? "\r\n" : "\n";
  const lines = text.split(/\r?\n/);
  let headerEnd = 0;
  while (headerEnd < lines.length && lines[headerEnd].startsWith(";")) {
    headerEnd++;
  }
  return {
    eol,
    header: lines.slice(0, headerEnd),
    body: lines.slice(headerEnd).join("\n").trim(),
  };
}

let shippedProjects = 0;
for (const project of readdirSync(srcDir, { withFileTypes: true })) {
  if (!project.isDirectory()) {
    continue;
  }

  const unshippedPath = join(srcDir, project.name, "AnalyzerReleases.Unshipped.md");
  const shippedPath = join(srcDir, project.name, "AnalyzerReleases.Shipped.md");
  if (!existsSync(unshippedPath)) {
    continue;
  }
  if (!existsSync(shippedPath)) {
    throw new Error(`${relative(repoRoot, unshippedPath)} has no AnalyzerReleases.Shipped.md next to it.`);
  }

  const unshipped = read(unshippedPath);
  if (!unshipped.body) {
    continue;
  }

  const shipped = read(shippedPath);
  const releaseHeading = `## Release ${version}`;
  if (shipped.body.split("\n").includes(releaseHeading)) {
    throw new Error(`${relative(repoRoot, shippedPath)} already has a "${releaseHeading}" section.`);
  }

  const sections = [shipped.body, `${releaseHeading}\n\n${unshipped.body}`].filter(Boolean);
  const shippedText = [...shipped.header, "", sections.join("\n\n")].join("\n") + "\n";
  writeFileSync(shippedPath, shippedText.replace(/\n/g, shipped.eol));
  writeFileSync(unshippedPath, (unshipped.header.join("\n") + "\n").replace(/\n/g, unshipped.eol));
  shippedProjects++;
  console.log(`${project.name}: shipped analyzer rules as ${releaseHeading}`);
}

if (shippedProjects === 0) {
  console.log("No unshipped analyzer rules; nothing to ship.");
}
