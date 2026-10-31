export interface Product {
  id: number;
  name: string;
  imagePath: string;
  thumbnailPath: string;
  startingPrice: number;
  currentPrice: number;
  bidCount: number;
  winnerPhone: string | null;
  publisher: string;
  collection: string;
  equipo: string;
  tipoCromo: string;
  isNumbered: boolean;
  numbering: string | null;
  pagado: boolean;
  enviado: boolean;
  auctionEndTime: string;
}
