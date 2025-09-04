import hre from "hardhat";
import "@nomicfoundation/hardhat-ethers"; 

async function main() {
  const Contest = await hre.ethers.getContractFactory("KeynesianBeautyContest");
  const contest = await Contest.deploy();

  await contest.waitForDeployment();

  console.log("Contest deployed to:", await contest.getAddress());
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
