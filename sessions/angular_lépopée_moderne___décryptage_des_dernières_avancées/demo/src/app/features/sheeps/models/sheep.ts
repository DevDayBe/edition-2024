export interface Sheep {
    id: number;              // Unique identifier for the sheep
    name: string;            // Name of the sheep
    age: number;             // Age of the sheep in years
    breed: string;           // Breed of the sheep
    woolColor: string;       // Color of the sheep's wool
    isSheared: boolean;      // Indicates whether the sheep has been sheared or not
    weight: number;          // Weight of the sheep in kilograms
    healthStatus?: string;   // Optional: current health condition of the sheep (e.g., "healthy", "sick")
    imageUrl: string;
    description?:string;
    temperament: 'Calm' | 'Aggressive' | 'Playful';
}