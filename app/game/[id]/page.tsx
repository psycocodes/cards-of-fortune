import GameWrapper from "@/components/GameWrapper";

interface GamePageProps {
  params: {
    id: string;
  };
}

export default async function GamePage({ params }: GamePageProps) {
  return (
    <div className="w-screen h-screen bg-black">
      <div id="unity-container" className="w-full h-full">
        <GameWrapper gameId={params.id} />
      </div>
    </div>
  );
}
