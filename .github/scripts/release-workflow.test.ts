import { describe, expect, test } from "bun:test";
import { readFileSync } from "node:fs";

const loadWorkflow = (name: string) =>
  Bun.YAML.parse(
    readFileSync(new URL(`../workflows/${name}`, import.meta.url), "utf8"),
  ) as Record<string, any>;

const stepByName = (job: Record<string, any>, name: string) =>
  job.steps.find((step: Record<string, any>) => step.name === name);

describe("release workflow trust boundary", () => {
  test("keeps the historical tag workflow inert", () => {
    const workflow = loadWorkflow("release.yml");

    expect(workflow.on.push).toBeUndefined();
    expect(workflow.on.workflow_dispatch).toBeDefined();
    expect(workflow.permissions).toEqual({});
    expect(workflow.jobs.retired).toBeDefined();
  });

  test("keeps release requests read-only", () => {
    const workflow = loadWorkflow("release-request.yml");
    const request = workflow.jobs.request;
    const validation = stepByName(request, "Validate release request");
    const upload = stepByName(request, "Upload release request");

    expect(workflow.on.workflow_dispatch.inputs.version.required).toBe(true);
    expect(workflow.permissions).toEqual({ contents: "read" });
    expect(validation.run).toContain("refs/heads/$env:DEFAULT_BRANCH");
    expect(validation.run).toContain("[0-9]");
    expect(validation.run).not.toContain("\\d");
    expect(upload.with["retention-days"]).toBe(1);
  });

  test("publishes only through trusted workflow_run code and gated jobs", () => {
    const workflow = loadWorkflow("publish-release.yml");
    const jobs = workflow.jobs;
    const verify = jobs["verify-release"];
    const build = jobs["build-msix"];
    const publish = jobs["publish-release"];
    const verification = stepByName(verify, "Verify trusted main request");
    const targetRecheck = stepByName(publish, "Recheck release target");
    const tagCreation = stepByName(
      publish,
      "Create release tag at the verified SHA",
    );

    expect(workflow.on.workflow_run.workflows).toEqual(["Release Request"]);
    expect(workflow.permissions).toEqual({ contents: "read" });
    expect(verify.if).toContain(
      "github.event.workflow_run.head_branch == github.event.repository.default_branch",
    );
    expect(verification.run).toContain(
      "$env:REQUESTED_SHA -ne $mainSha",
    );
    expect(verification.run).toContain("[0-9]");
    expect(verification.run).not.toContain("\\d");

    expect(build.needs).toBe("verify-release");
    expect(build.strategy.matrix.platform).toEqual(["x64", "ARM64"]);
    expect(build.uses).toBe("./.github/workflows/build-msix.yml");
    expect(build.with.configuration).toBe("Release");

    expect(publish.needs).toEqual(["verify-release", "build-msix"]);
    expect(publish.permissions).toEqual({ contents: "write" });
    expect(targetRecheck.run).toContain("$env:RELEASE_SHA -ne $mainSha");
    expect(tagCreation.with.script).toContain("github.rest.git.createRef");
    expect(tagCreation.with.script).toContain(
      "sha: process.env.RELEASE_SHA",
    );

    const writeJobs = Object.values(jobs)
      .filter((job: any) => job.permissions?.contents === "write")
      .map((job: any) => job.name);
    expect(writeJobs).toEqual(["publish-release"]);
  });
});
