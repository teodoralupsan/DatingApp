import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { User } from '../_models/user';
import { Photo } from '../_models/photo';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getUsersWithRoles() {
    return this.http.get(`${this.baseUrl}admin/getUsersWithRoles`);
  }

  updateUserRoles(user: User, roles: {}) {
    return this.http.post(`${this.baseUrl}admin/editRoles/${user.userName}`, roles);
  }

  getPhotosForModerators() {
    return this.http.get(`${this.baseUrl}admin/getPhotosForModerators`);
  }

  approvePhoto(photoId: number) {
    return this.http.post(`${this.baseUrl}admin/approvePhoto/${photoId}`, {});
  }

  deletePhoto(photoId: number) {
    return this.http.delete(`${this.baseUrl}admin/rejectPhoto/${photoId}`);
  }

}
