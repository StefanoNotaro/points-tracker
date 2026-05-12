export type SportType = 'volleyball' | 'beach_volleyball';

export interface SportConfig {
  type: SportType;
  label: string;
  icon: string;
  pointsPerSet: number;
  lastSetPoints: number;
  setsToWin: number;
  totalSets: number;
  winByTwo: boolean;
}

export const SPORT_CONFIGS: Record<SportType, SportConfig> = {
  volleyball: {
    type: 'volleyball',
    label: 'Volleyball',
    icon: 'sports_volleyball',
    pointsPerSet: 25,
    lastSetPoints: 15,
    setsToWin: 3,
    totalSets: 5,
    winByTwo: true,
  },
  beach_volleyball: {
    type: 'beach_volleyball',
    label: 'Beach Volleyball',
    icon: 'beach_access',
    pointsPerSet: 21,
    lastSetPoints: 15,
    setsToWin: 2,
    totalSets: 3,
    winByTwo: true,
  },
};
