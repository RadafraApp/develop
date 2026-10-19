export interface Product {
  id: number;
  name: string;
  imagePath: string;
  startingPrice: number;
  currentPrice: number;
  bidCount: number;
  publisher: string;
  collection: string;
  isNumbered: boolean;
  numbering: string | null;
  auctionEndTime: string;
}
