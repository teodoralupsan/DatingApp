import { Photo } from './photo';

export interface User {
    id: number;
    username: string;
    knownAs: string;
    age: number;
    gender: string;
    created: Date;
    lastActive: Date;
    photUrl: string;
    city: string;
    country: string;
    interests?: string;
    introduction?: string;
    lookingFpr?: string;
    photos?: Photo[];
}
