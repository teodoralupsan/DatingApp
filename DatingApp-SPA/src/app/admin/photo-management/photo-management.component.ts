import { Component, OnInit } from '@angular/core';
import { Photo } from 'src/app/_models/photo';
import { AdminService } from 'src/app/_services/admin.service';
import { AlertifyService } from 'src/app/_services/alertify.service';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  photos: Photo[] = [];

  constructor(
    private adminService: AdminService,
    private alertify: AlertifyService) { }

  ngOnInit() {
    this.getPhotosForModerators();
  }

  getPhotosForModerators() {
    this.adminService.getPhotosForModerators().subscribe((photoList: Photo[]) => {
      this.photos = photoList;
    }, error => {
      this.alertify.error(error);
    });
  }

  approve(photoId: number) {
    this.adminService.approvePhoto(photoId).subscribe(() => {
      this.getPhotosForModerators();
    }, error => {
      this.alertify.error(error);
    });
  }

  reject(photoId: number) {
    this.adminService.deletePhoto(photoId).subscribe(() => {
      this.getPhotosForModerators();
    }, error => {
      this.alertify.error(error);
    });
  }
}

